using System.Text.Json;
using BookLibrary.Models;

namespace BookLibrary.Services;

public abstract class BookAiProviderBase : IBookAiService
{
    protected readonly ILogger Logger;

    protected BookAiProviderBase(ILogger logger) => Logger = logger;

    protected abstract string Name { get; }

    protected abstract bool HasWebSearch { get; }

    protected abstract string? ConfigurationError { get; }

    public bool IsConfigured => ConfigurationError is null;

    protected abstract Task<AiResult<string>> CompleteRecommendationsAsync(string prompt, CancellationToken ct);

    protected abstract Task<AiResult<string>> ReadCoverAsync(byte[] imageBytes, string mimeType, CancellationToken ct);

    protected abstract Task<AiResult<string>> CompleteCoverDetailsAsync(string prompt, CancellationToken ct);

    public Task<AiResult<IReadOnlyList<BookRecommendation>>> GetBookRecommendationsAsync(
        IEnumerable<Book> userBooks, int count = 6, CancellationToken cancellationToken = default) =>
        GuardAsync<IReadOnlyList<BookRecommendation>>(async ct =>
        {
            var prompt = AiPrompts.Recommendations(userBooks?.ToList() ?? new List<Book>(), count);

            var response = await CompleteRecommendationsAsync(prompt, ct);
            if (!response.IsSuccess) return Forward<IReadOnlyList<BookRecommendation>>(response);

            var items = AiResponseParser.ParseRecommendations(response.Data!, count);
            return items.Count == 0
                ? AiResult<IReadOnlyList<BookRecommendation>>.Fail(
                    AiErrorKind.NotRecognized, $"{Name} не върна препоръки.", response.Provider)
                : AiResult<IReadOnlyList<BookRecommendation>>.Success(items, response.Provider);
        }, cancellationToken);

    public Task<AiResult<BookCoverInfo>> RecognizeCoverAsync(
        byte[] imageBytes, string mimeType, IReadOnlyList<string> genres, CancellationToken cancellationToken = default)
    {
        if (imageBytes is null || imageBytes.Length == 0)
            return Task.FromResult(AiResult<BookCoverInfo>.Fail(AiErrorKind.InvalidInput, "Моля, изберете снимка на корицата."));

        return GuardAsync<BookCoverInfo>(async ct =>
        {
            var step1 = await ReadCoverAsync(imageBytes, mimeType, ct);
            if (!step1.IsSuccess) return Forward<BookCoverInfo>(step1);

            var read = AiResponseParser.ParseObject(step1.Data!);
            var title = AiResponseParser.GetString(read, "title");
            var author = AiResponseParser.GetString(read, "author");

            if (string.IsNullOrWhiteSpace(title))
                return AiResult<BookCoverInfo>.Fail(AiErrorKind.NotRecognized,
                    "Не успях да прочета заглавието от снимката. Опитайте с по-ясна снимка, без отблясъци.",
                    step1.Provider);

            var step2 = await CompleteCoverDetailsAsync(AiPrompts.CoverDetails(title, author, genres, HasWebSearch), ct);
            if (!step2.IsSuccess)
            {
                return AiResult<BookCoverInfo>.Success(
                    new BookCoverInfo { Title = title, Author = author },
                    step1.Provider,
                    "Заглавието и авторът са разпознати, но търсенето на допълнителна информация не успя. " +
                    "Допълнете останалите полета ръчно.");
            }

            var info = AiResponseParser.ToCoverInfo(AiResponseParser.ParseObject(step2.Data!), title, author, genres);
            return HasWebSearch
                ? AiResult<BookCoverInfo>.Success(info, $"{step2.Provider} + търсене в интернет")
                : AiResult<BookCoverInfo>.Success(info, step2.Provider,
                    "Данните са попълнени от модела без търсене в интернет. Моля, проверете ги.");
        }, cancellationToken);
    }

    private async Task<AiResult<T>> GuardAsync<T>(Func<CancellationToken, Task<AiResult<T>>> action, CancellationToken ct)
    {
        if (ConfigurationError is { } configError)
            return AiResult<T>.Fail(AiErrorKind.NotConfigured, configError, Name);

        try
        {
            return await action(ct);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogWarning(ex, "Мрежова грешка при връзка с {Provider}.", Name);
            return AiResult<T>.Fail(AiErrorKind.Unavailable, $"Няма връзка с {Name}. Проверете връзката.", Name);
        }
        catch (Exception ex) when (ex is TaskCanceledException or TimeoutException && !ct.IsCancellationRequested)
        {
            Logger.LogWarning(ex, "Времето за изчакване на {Provider} изтече.", Name);
            return AiResult<T>.Fail(AiErrorKind.Unavailable, $"{Name} не отговори навреме. Опитайте отново.", Name);
        }
        catch (JsonException ex)
        {
            Logger.LogWarning(ex, "Невалиден JSON от {Provider}.", Name);
            return AiResult<T>.Fail(AiErrorKind.InvalidResponse,
                $"{Name} върна отговор в неочакван формат. Опитайте отново.", Name);
        }
    }

    private static AiResult<T> Forward<T>(AiResult<string> failed) =>
        AiResult<T>.Fail(failed.ErrorKind, failed.Error!, failed.Provider);
}
