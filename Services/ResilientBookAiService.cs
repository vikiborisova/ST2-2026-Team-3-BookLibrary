using BookLibrary.Models;

namespace BookLibrary.Services;

// Оркестратор, който управлява fallback логиката между локалния AI и Gemini
public sealed class ResilientBookAiService : IBookAiService
{
    public const string LocalAiCircuit = "LocalAi";

    private readonly LocalAiService _local;
    private readonly GeminiAiService _gemini;
    private readonly CircuitBreaker _breaker;
    private readonly ILogger<ResilientBookAiService> _logger;

    public ResilientBookAiService(
        LocalAiService local,
        GeminiAiService gemini,
        [FromKeyedServices(LocalAiCircuit)] CircuitBreaker breaker,
        ILogger<ResilientBookAiService> logger)
    {
        _local = local;
        _gemini = gemini;
        _breaker = breaker;
        _logger = logger;
    }

    public Task<AiResult<IReadOnlyList<BookRecommendation>>> GetBookRecommendationsAsync(
        IEnumerable<Book> userBooks, int count = 6, CancellationToken cancellationToken = default)
    {
        var books = userBooks?.ToList() ?? new List<Book>();
        return ExecuteAsync(
            ct => _local.GetBookRecommendationsAsync(books, count, ct),
            ct => _gemini.GetBookRecommendationsAsync(books, count, ct),
            cancellationToken);
    }

    public Task<AiResult<BookCoverInfo>> RecognizeCoverAsync(
        byte[] imageBytes, string mimeType, IReadOnlyList<string> genres, CancellationToken cancellationToken = default)
    {
        if (imageBytes is null || imageBytes.Length == 0)
            return Task.FromResult(AiResult<BookCoverInfo>.Fail(AiErrorKind.InvalidInput, "Моля, изберете снимка на корицата."));
        // Опит за използване на локалния AI, с fallback към Gemini
        return ExecuteAsync(
            ct => _local.RecognizeCoverAsync(imageBytes, mimeType, genres, ct),
            ct => _gemini.RecognizeCoverAsync(imageBytes, mimeType, genres, ct),
            cancellationToken);
    }

    // Централизирана логика за превключване между доставчиците
    private async Task<AiResult<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<AiResult<T>>> local,
        Func<CancellationToken, Task<AiResult<T>>> fallback,
        CancellationToken ct)
    {
        // Проверяваме дали локалният AI въобще е активиран/конфигуриран
        if (_local.IsConfigured)
        {
            try
            {// Завиваме извикването на локалния AI в Circuit Breaker-а.
                var result = await _breaker.ExecuteAsync(local, r => r.ErrorKind == AiErrorKind.Unavailable, ct);
                // Ако локалният AI върне успешен резултат, го връщаме директно
                if (result.IsSuccess) return result;

                _logger.LogWarning("Local AI failed ({Kind}): {Error} Falling back to Gemini.", result.ErrorKind, result.Error);
            }
            catch (CircuitBrokenException ex)
            // Ако локалният AI е паднал, не чакаме, а директно логваме и минаваме на Gemini
            {
                _logger.LogDebug("Local AI circuit is open (retry after {RetryAfter}). Using Gemini.", ex.RetryAfter);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {// При непредвидени системни грешки в локалната мрежа също минаваме на Gemini
                _logger.LogWarning(ex, "Local AI threw unexpectedly. Falling back to Gemini.");
            }
        }
        // Ако локалният AI е изключен, паднал или върнал грешка, изпълняваме заявката към Google Gemini
        return await fallback(ct);
    }
}
