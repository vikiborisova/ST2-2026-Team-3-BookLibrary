namespace BookLibrary.Services;

//списък с възможни категории грешки
public enum AiErrorKind
{
    None,
    InvalidInput,
    NotConfigured,
    Unavailable,
    InvalidResponse,
    NotRecognized
}

public sealed class AiResult<T>
{
    public T? Data { get; init; } //резултатът
    public string? Provider { get; init; } //Gemini
    public string? Notice { get; init; }
    public string? Error { get; init; }
    public AiErrorKind ErrorKind { get; init; }

    public bool IsSuccess => ErrorKind == AiErrorKind.None;

    //Помощни фабрични методи за бързо създаване на резултат
    public static AiResult<T> Success(T data, string? provider, string? notice = null) =>
        new() { Data = data, Provider = provider, Notice = notice };

    public static AiResult<T> Fail(AiErrorKind kind, string error, string? provider = null) =>
        new() { ErrorKind = kind, Error = error, Provider = provider };
}
