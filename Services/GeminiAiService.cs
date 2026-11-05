using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using BookLibrary.Configuration;
using Microsoft.Extensions.Options;

namespace BookLibrary.Services;

public sealed class GeminiAiService : BookAiProviderBase
{
    public const string BaseUrl = "https://generativelanguage.googleapis.com/";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly HttpClient _http;
    private readonly ExternalApisOptions _options;

    public GeminiAiService(HttpClient http, IOptions<ExternalApisOptions> options, ILogger<GeminiAiService> logger)
        : base(logger)
    {
        _http = http;
        _options = options.Value;
    }

    protected override string Name => "Gemini";
    protected override bool HasWebSearch => true;

    protected override string? ConfigurationError =>
        string.IsNullOrWhiteSpace(_options.GeminiApiKey)
            ? "Не е зададен Gemini API ключ (ExternalApis:GeminiApiKey)."
            : null;

    protected override Task<AiResult<string>> CompleteRecommendationsAsync(string prompt, CancellationToken ct) =>
        SendAsync(new
        {
            contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
            generationConfig = new
            {
                temperature = 0.7,
                responseMimeType = "application/json",
                responseSchema = new
                {
                    type = "ARRAY",
                    items = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            title = new { type = "STRING" },
                            author = new { type = "STRING" },
                            description = new { type = "STRING" }
                        },
                        required = new[] { "title", "author", "description" }
                    }
                }
            }
        }, ct);

    protected override Task<AiResult<string>> ReadCoverAsync(byte[] imageBytes, string mimeType, CancellationToken ct) =>
        SendAsync(new
        {
            systemInstruction = new { parts = new[] { new { text = AiPrompts.CoverVisionSystem } } },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new object[]
                    {
                        new { inlineData = new { mimeType, data = Convert.ToBase64String(imageBytes) } },
                        new { text = AiPrompts.CoverVisionUser }
                    }
                }
            },
            generationConfig = new { temperature = 0.1, responseMimeType = "application/json" }
        }, ct);

    protected override Task<AiResult<string>> CompleteCoverDetailsAsync(string prompt, CancellationToken ct) =>
        SendAsync(new
        {
            contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
            tools = new[] { new { google_search = new { } } },
            generationConfig = new { temperature = 0.2 }
        }, ct);

    private async Task<AiResult<string>> SendAsync(object payload, CancellationToken ct)
    {
        var model = _options.GeminiModel.Trim();
        var provider = $"Gemini ({model})";

        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"v1beta/models/{Uri.EscapeDataString(model)}:generateContent");
        request.Headers.Add("x-goog-api-key", _options.GeminiApiKey!.Trim());
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (response.IsSuccessStatusCode)
        {
            var text = ReadText(body);
            return string.IsNullOrWhiteSpace(text)
                ? AiResult<string>.Fail(AiErrorKind.InvalidResponse, "Gemini върна празен отговор.", provider)
                : AiResult<string>.Success(text, provider);
        }

        var status = response.StatusCode;
        Logger.LogWarning("Gemini модел {Model} върна {Status}: {Body}", model, (int)status, body);

        var error = status switch
        {
            HttpStatusCode.TooManyRequests => "Лимитът на заявките към Gemini е достигнат. Опитайте след малко.",
            HttpStatusCode.ServiceUnavailable => "Сървърите на Gemini са временно претоварени. Опитайте след няколко секунди.",
            HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                "Gemini отхвърли заявката или API ключа. Проверете ExternalApis:GeminiApiKey.",
            _ => $"Грешка от Gemini (HTTP {(int)status}). Опитайте отново."
        };
        var kind = status is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
            ? AiErrorKind.NotConfigured
            : AiErrorKind.Unavailable;

        return AiResult<string>.Fail(kind, error, provider);
    }

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
}
