namespace BookLibrary.Services;

// Състоянията на прекъсвача
public enum CircuitState
{
    Closed,

    Open,

    HalfOpen
}

// Изключение, което се хвърля, когато веригата е отворена (услугата е маркирана като паднала)
public sealed class CircuitBrokenException : Exception
{

    public CircuitBrokenException(string circuitName, TimeSpan retryAfter)
        : base($"Circuit '{circuitName}' is open. Retry after {retryAfter.TotalSeconds:0}s.")
    {
        CircuitName = circuitName;
        RetryAfter = retryAfter;
    }

    public string CircuitName { get; }
    public TimeSpan RetryAfter { get; }
}

public sealed class CircuitBreakerOptions
{
    // Брой последователни грешки преди веригата да се отвори
    public int FailureThreshold { get; set; } = 3;

    // Време в секунди, през което заявките ще бъдат директно отхвърляни, преди да се опита отново
    public int BreakDurationSeconds { get; set; } = 60;
}

public sealed class CircuitBreaker
{
    private readonly object _gate = new();
    private readonly int _failureThreshold;
    private readonly TimeSpan _breakDuration;
    private readonly TimeProvider _time;
    private readonly ILogger _logger;

    private CircuitState _state = CircuitState.Closed;
    private int _consecutiveFailures;
    private DateTimeOffset _openedAt;
    private bool _trialInFlight;

    public CircuitBreaker(string name, CircuitBreakerOptions options, TimeProvider time, ILogger logger)
    {
        Name = name;
        _failureThreshold = Math.Max(1, options.FailureThreshold);
        _breakDuration = TimeSpan.FromSeconds(Math.Max(1, options.BreakDurationSeconds));
        _time = time;
        _logger = logger;
    }

    public string Name { get; }

    public CircuitState State
    {
        get { lock (_gate) return _state; }
    }

    // Основният метод, който извиква уязвимата услуга
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        Func<T, bool> isFailure,
        CancellationToken cancellationToken = default)
    {
        // Проверява дали имаме право да изпълним заявката, спрямо състоянието на прекъсвача
        AcquirePermission();
        AcquirePermission();

        var recorded = false;
        try
        {
            // Изпълнение на самата операция
            var result = await action(cancellationToken).ConfigureAwait(false);
            // Проверка дали резултатът представлява неуспех за услугата
            if (isFailure(result)) OnFailure(); else OnSuccess();
            recorded = true;
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // При хвърлено изключение, маркираме опитът като неуспешен
            OnFailure();
            recorded = true;
            throw;
        }
        finally
        {
            if (!recorded) ReleaseTrial();
        }
    }

    private void AcquirePermission()
    {
        lock (_gate)
        {
            switch (_state)
            {
                case CircuitState.Closed:
                    return;

                case CircuitState.Open:
                    var elapsed = _time.GetUtcNow() - _openedAt;
                    // Ако не е минало достатъчно време, директно хвърляме грешка без да бавим системата
                    if (elapsed < _breakDuration)
                        if (elapsed < _breakDuration)
                        throw new CircuitBrokenException(Name, _breakDuration - elapsed);

                    // Времето е изтекло, преминаваме в полу-отворено състояние за проба
                    _state = CircuitState.HalfOpen;
                    _trialInFlight = true;
                    _logger.LogInformation("Circuit {Circuit} is half-open; sending a trial call.", Name);
                    return;

                // Ако вече има пусната пробна заявка, отхвърляме останалите докато не видим резултата от нея
                case CircuitState.HalfOpen:
                    if (_trialInFlight)
                        throw new CircuitBrokenException(Name, TimeSpan.Zero);

                    _trialInFlight = true;
                    return;
            }
        }
    }

    private void OnSuccess()
    {
        lock (_gate)
        {
            if (_state == CircuitState.Open) return;

            if (_state == CircuitState.HalfOpen)
                _logger.LogInformation("Circuit {Circuit} closed; trial call succeeded.", Name);

            // Възстановяваме нормалното състояние при успех
            _state = CircuitState.Closed;
            _consecutiveFailures = 0;
            _trialInFlight = false;
        }
    }

    private void OnFailure()
    {
        lock (_gate)
        {
            switch (_state)
            {
                case CircuitState.Open:
                    return;

                case CircuitState.HalfOpen:
                    // Ако пробната заявка се провали, директно затваряме веригата отново
                    Trip("trial call failed");
                    return;

                case CircuitState.Closed:
                    // Увеличаваме брояча. Ако стигне лимита, отваряме веригата.
                    if (++_consecutiveFailures >= _failureThreshold)
                        Trip($"{_consecutiveFailures} consecutive failures");
                    return;
            }
        }
    }

    private void ReleaseTrial()
    {
        lock (_gate) _trialInFlight = false;
    }

    // блокира следващите заявки
    private void Trip(string reason)
    {
        _state = CircuitState.Open;
        _openedAt = _time.GetUtcNow();
        _consecutiveFailures = 0;
        _trialInFlight = false;
        _logger.LogWarning("Circuit {Circuit} opened for {Seconds}s: {Reason}.", Name, _breakDuration.TotalSeconds, reason);
    }
}
