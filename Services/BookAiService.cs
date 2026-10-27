using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using BookLibrary.Configuration;
using BookLibrary.Models;
using Microsoft.Extensions.Options;

namespace BookLibrary.Services
{
    public class BookAiService : IBookAiService
    {
        private readonly HttpClient _http;
        private readonly ExternalApisOptions _options;
        private readonly ILogger<BookAiService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private const string JsonInstructions =
            "Върни САМО JSON масив от обекти с полета: " +
            "\"title\" (заглавие на български), \"author\" (автор) и " +
            "\"description\" (2-3 изречения на български език защо книгата ще му хареса).";

        public BookAiService(HttpClient http, IOptions<ExternalApisOptions> options, ILogger<BookAiService> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<IReadOnlyList<BookRecommendation>> GetBookRecommendationsAsync(
            IEnumerable<Book> userBooks, int count = 6, CancellationToken cancellationToken = default)
        {
            var apiKey = _options.GeminiApiKey?.Trim();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Липсва Gemini API ключ в конфигурацията.");
                return Array.Empty<BookRecommendation>();
            }

            var model = string.IsNullOrWhiteSpace(_options.GeminiModel) ? "gemini-3-flash-preview" : _options.GeminiModel.Trim();
            var booksList = userBooks?.ToList() ?? new List<Book>();

            string prompt;
            if (booksList.Count > 0)
            {
                var bookTitles = string.Join("; ", booksList.Select(b => $"\"{b.Title}\" от {b.Author} ({b.Genre})"));
                prompt = $"Потребителят има следните книги на своя личен рафт: {bookTitles}. " +
                         $"На базата на тези жанрове и автори препоръчай точно {count} нови книги, " +
                         $"които НЕ са сред изброените. " + JsonInstructions;
            }
            else
            {
                prompt = $"Потребителят все още няма книги на своя рафт. Предложи точно {count} разнообразни " +
                         $"популярни книги към момента - световни и български бестселъри. " + JsonInstructions;
            }

            var requestBody = new
            {
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = prompt } } }
                },
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
            };

            var payload = JsonSerializer.Serialize(requestBody, JsonOptions);
            var endpoint = $"v1beta/models/{Uri.EscapeDataString(model)}:generateContent";

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };
                // ключът е в header, а не в URL, за да не попада в логовете на HttpClient
                request.Headers.Add("x-goog-api-key", apiKey);

                using var response = await _http.SendAsync(request, cancellationToken);
                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gemini върна {Status}: {Body}", (int)response.StatusCode, responseText);
                    return Array.Empty<BookRecommendation>();
                }

                using var doc = JsonDocument.Parse(responseText);
                var parts = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts");

                var text = string.Concat(parts.EnumerateArray()
                    .Select(p => p.TryGetProperty("text", out var t) ? t.GetString() : null));

                var result = JsonSerializer.Deserialize<List<BookRecommendation>>(StripFences(text), JsonOptions)
                             ?? new List<BookRecommendation>();

                return result
                    .Where(r => !string.IsNullOrWhiteSpace(r.Title))
                    .Select(r => new BookRecommendation
                    {
                        Title = r.Title.Trim(),
                        Author = r.Author?.Trim() ?? "",
                        Description = r.Description?.Trim() ?? ""
                    })
                    .Take(count)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Грешка при връзка с Gemini за книжни препоръки.");
                return Array.Empty<BookRecommendation>();
            }
        }

        // За всеки случай, ако моделът върне ```json ... ```
        private static string StripFences(string text)
        {
            text = text.Trim();
            if (text.StartsWith("```"))
            {
                var nl = text.IndexOf('\n');
                if (nl >= 0) text = text[(nl + 1)..];
                text = text.TrimEnd('`', '\n', '\r', ' ');
            }
            return text;
        }
    }
}