using System.Net;
using System.Text;
using System.Text.Json;

namespace BookLibrary.Services;

/// Резултат от разпознаването на корица (Result Object).</>
public sealed class BookCoverResult
{
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string? Genre { get; init; }
    public int? Year { get; init; }
    public string? Description { get; init; }
    public string? Provider { get; init; }
    public string? Notice { get; init; }
    public string? Error { get; init; }

    public bool IsSuccess => Error is null;

    public static BookCoverResult Fail(string error) => new() { Error = error };
}

public interface IBookCoverService
{
    Task<BookCoverResult> RecognizeAsync(
        byte[] imageBytes,
        string mimeType,
        IReadOnlyList<string> genres,
        CancellationToken cancellationToken = default);
}

public sealed class BookCoverService : IBookCoverService
{
    private static readonly string[] FallbackModels = { "gemini-2.5-flash", "gemini-2.5-flash-lite" };

    private const string VisionPrompt = """
        Ти разпознаваш книги от снимка на корица.
        Прочети заглавието и автора, които са написани на корицата.
        Върни САМО валиден JSON обект: {"title": "...", "author": "..."}
        Правила:
        1. Запази езика и азбуката, с които са изписани на корицата.
        2. Ако не се чете заглавие или автор, върни за него празен низ "".
        3. Не измисляй и не допълвай липсващи данни.
        """;

    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<BookCoverService> _logger;

    public BookCoverService(HttpClient http, IConfiguration config, ILogger<BookCoverService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<BookCoverResult> RecognizeAsync(
        byte[] imageBytes,
        string mimeType,
        IReadOnlyList<string> genres,
        CancellationToken cancellationToken = default)
    {
        if (imageBytes is null || imageBytes.Length == 0)
            return BookCoverResult.Fail("Моля, изберете снимка на корицата.");

        var apiKey = _config["Gemini:ApiKey"]?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
            return BookCoverResult.Fail("Не е зададен Gemini API ключ (Gemini:ApiKey в appsettings.json).");

        var models = new List<string>();
        var configured = _config["Gemini:Model"]?.Trim();
        if (!string.IsNullOrWhiteSpace(configured)) models.Add(configured);
        models.AddRange(FallbackModels);
        models = models.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        try
        {
            // ---------- Етап 1: четене на заглавие и автор от корицата ----------
            var visionPayload = new
            {
                systemInstruction = new { parts = new[] { new { text = VisionPrompt } } },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { inlineData = new { mimeType, data = Convert.ToBase64String(imageBytes) } },
                            new { text = "Прочети заглавието и автора от корицата на тази книга." }
                        }
                    }
                },
                generationConfig = new { temperature = 0.1, responseMimeType = "application/json" }
            };

            var step1 = await SendAsync(models, apiKey, visionPayload, cancellationToken);
            if (step1.Error is not null) return BookCoverResult.Fail(step1.Error);

            var read = ParseJson(step1.Text!);
            var title = GetString(read, "title");
            var author = GetString(read, "author");

            if (string.IsNullOrWhiteSpace(title))
                return BookCoverResult.Fail("Не успях да прочета заглавието от снимката. Опитайте с по-ясна снимка, без отблясъци.");

            var provider = $"Gemini ({step1.Model})";

            // ---------- Етап 2: търсене в интернет (Google Search grounding) ----------
            var searchPrompt =
                $"Намери в интернет информация за книгата „{title}“" +
                (string.IsNullOrWhiteSpace(author) ? "" : $" от {author}") + ".\n" +
                "Върни САМО JSON обект, без Markdown: " +
                "{\"title\": \"...\", \"author\": \"...\", \"genre\": \"...\", \"year\": 1999, \"description\": \"...\"}\n" +
                "Правила:\n" +
                "1. title и author са официалното изписване, на езика на корицата.\n" +
                "2. genre е точно един от списъка: " + string.Join(", ", genres) + ". Ако нито един не пасва, върни \"\".\n" +
                "3. year е годината на първото издание като число, или null.\n" +
                "4. description са 2 до 3 изречения на български, без спойлери.\n" +
                "5. Ако не си сигурен, остави празен низ. Не измисляй.";

            var searchPayload = new
            {
                contents = new[] { new { role = "user", parts = new[] { new { text = searchPrompt } } } },
                tools = new[] { new { google_search = new { } } },
                generationConfig = new { temperature = 0.2 }
            };

            var step2 = await SendAsync(models, apiKey, searchPayload, cancellationToken);

            if (step2.Error is not null)
            {
                // Корицата е прочетена, само търсенето не успя.
                return new BookCoverResult
                {
                    Title = title,
                    Author = author,
                    Provider = provider,
                    Notice = "Заглавието и авторът са разпознати, но търсенето в интернет не успя. Допълнете останалите полета ръчно."
                };
            }

            var found = ParseJson(step2.Text!);

            var foundGenre = GetString(found, "genre");
            var matchedGenre = genres.FirstOrDefault(g => string.Equals(g, foundGenre, StringComparison.OrdinalIgnoreCase));

            var description = GetString(found, "description");
            if (description.Length > 800) description = description[..800];

            return new BookCoverResult
            {
                Title = FirstNonEmpty(GetString(found, "title"), title),
                Author = FirstNonEmpty(GetString(found, "author"), author),
                Genre = matchedGenre,
                Year = GetYear(found),
                Description = string.IsNullOrWhiteSpace(description) ? null : description,
                Provider = $"{provider} + търсене в Google"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Мрежова грешка при връзка с Gemini.");
            return BookCoverResult.Fail("Няма връзка с Gemini. Проверете интернет връзката.");
        }
        catch (Exception ex) when (ex is TaskCanceledException or TimeoutException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Времето за изчакване на Gemini изтече.");
            return BookCoverResult.Fail("Gemini не отговори навреме. Опитайте отново.");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Невалиден JSON от Gemini.");
            return BookCoverResult.Fail("Gemini върна отговор в неочакван формат. Опитайте отново.");
        }
    }

    // Верига от модели: при 503 / 429 / 404 се опитва със следващия (Chain of Responsibility).
    private async Task<(string? Text, string? Error, string? Model)> SendAsync(
        IEnumerable<string> models, string apiKey, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload);
        var last = HttpStatusCode.ServiceUnavailable;

        foreach (var model in models)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent");
            request.Headers.Add("x-goog-api-key", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var text = ReadText(body);
                return string.IsNullOrWhiteSpace(text)
                    ? (null, "Gemini върна празен отговор.", model)
                    : (text, null, model);
            }

            last = response.StatusCode;
            _logger.LogWarning("Gemini модел {Model} върна {Status}: {Body}", model, (int)last, body);

            if (last is HttpStatusCode.ServiceUnavailable or HttpStatusCode.TooManyRequests or HttpStatusCode.NotFound)
                continue;

            return last is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                ? (null, "Gemini отхвърли заявката или API ключа. Проверете Gemini:ApiKey.", model)
                : (null, $"Грешка от Gemini (HTTP {(int)last}). Опитайте отново.", model);
        }

        return (null, last == HttpStatusCode.TooManyRequests
            ? "Лимитът на заявките към Gemini е достигнат. Опитайте след малко."
            : "Сървърите на Gemini са временно претоварени. Опитайте след няколко секунди.", null);
    }

    // Събира текста от всички части на първия кандидат.
    private static string? ReadText(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            return null;

        if (!candidates[0].TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts))
            return null;

        var sb = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
            if (part.TryGetProperty("text", out var t)) sb.Append(t.GetString());

        return sb.ToString();
    }

    // Изважда JSON обекта между първата { и последната } (маха Markdown огражденията).
    private static JsonElement ParseJson(string text)
    {
        var first = text.IndexOf('{');
        var last = text.LastIndexOf('}');
        var json = first >= 0 && last > first ? text[first..(last + 1)] : "{}";
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static string GetString(JsonElement el, string name) =>
        el.ValueKind == JsonValueKind.Object &&
        el.TryGetProperty(name, out var v) &&
        v.ValueKind == JsonValueKind.String
            ? (v.GetString() ?? string.Empty).Trim()
            : string.Empty;

    private static int? GetYear(JsonElement el)
    {
        if (el.ValueKind != JsonValueKind.Object || !el.TryGetProperty("year", out var v)) return null;

        int year;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out year)) { }
        else if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out year)) { }
        else return null;

        return year is >= 1000 and <= 2100 ? year : null;
    }

    private static string FirstNonEmpty(string a, string b) => string.IsNullOrWhiteSpace(a) ? b : a;
}