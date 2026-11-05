using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using BookLibrary.Configuration;
using Microsoft.Extensions.Options;

namespace BookLibrary.Services;

public sealed class LocalAiService : BookAiProviderBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly HttpClient _http;
    private readonly LocalAiOptions _options;

    public LocalAiService(HttpClient http, IOptions<LocalAiOptions> options, ILogger<LocalAiService> logger)
        : base(logger)
    {
        _http = http;
        _options = options.Value;
    }

    protected override string Name => "Локален AI";
    protected override bool HasWebSearch => false;

    protected override string? ConfigurationError =>
        !_options.Enabled ? "Локалният AI е изключен (LocalAi:Enabled)."
        : _http.BaseAddress is null ? "Не е зададен адрес на локалния AI (LocalAi:BaseUrl)."
        : string.IsNullOrWhiteSpace(_options.Model) ? "Не е зададен модел на локалния AI (LocalAi:Model)."
        : null;

    protected override Task<AiResult<string>> CompleteRecommendationsAsync(string prompt, CancellationToken ct) =>
        ChatAsync(new object[] { new { role = "user", content = prompt } }, temperature: 0.7, ct);

    protected override Task<AiResult<string>> ReadCoverAsync(byte[] imageBytes, string mimeType, CancellationToken ct) =>
        !_options.SupportsVision
            ? Task.FromResult(AiResult<string>.Fail(AiErrorKind.NotConfigured,
                "Локалният модел не поддържа изображения (LocalAi:SupportsVision).", Name))
            : ChatAsync(new object[]
        {
            new { role = "system", content = AiPrompts.CoverVisionSystem },
            new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "text", text = AiPrompts.CoverVisionUser },
                    new { type = "image_url", image_url = new { url = $"data:{mimeType};base64,{Convert.ToBase64String(imageBytes)}" } }
                }
            }
        }, temperature: 0.1, ct);

    protected override Task<AiResult<string>> CompleteCoverDetailsAsync(string prompt, CancellationToken ct) =>
        ChatAsync(new object[] { new { role = "user", content = prompt } }, temperature: 0.2, ct);

    private async Task<AiResult<string>> ChatAsync(object[] messages, double temperature, CancellationToken ct)
    {
        var model = _options.Model!.Trim();
        var provider = $"Локален AI ({model})";

        var payload = new Dictionary<string, object>
        {
            ["model"] = model,
            ["messages"] = messages,
            ["temperature"] = temperature,
            ["stream"] = false
        };
        if (_options.UseJsonResponseFormat)
            payload["response_format"] = new { type = "json_object" };

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
        };
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey.Trim());

        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            Logger.LogWarning("Локалният AI модел {Model} върна {Status}: {Body}", model, (int)response.StatusCode, body);

            return response.StatusCode == HttpStatusCode.BadRequest
                ? AiResult<string>.Fail(AiErrorKind.InvalidResponse, $"Локалният AI отхвърли заявката (HTTP 400).", provider)
                : AiResult<string>.Fail(AiErrorKind.Unavailable, $"Грешка от локалния AI (HTTP {(int)response.StatusCode}).", provider);
        }

        var text = ReadText(body);
        return string.IsNullOrWhiteSpace(text)
            ? AiResult<string>.Fail(AiErrorKind.InvalidResponse, "Локалният AI върна празен отговор.", provider)
            : AiResult<string>.Success(text, provider);
    }

    private static string? ReadText(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        if (!doc.RootElement.TryGetProperty("choices", out var choices) ||
            choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0)
            return null;

        return choices[0].TryGetProperty("message", out var message) &&
               message.TryGetProperty("content", out var content) &&
               content.ValueKind == JsonValueKind.String
            ? content.GetString()
            : null;
    }
}
