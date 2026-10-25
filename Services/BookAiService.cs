using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        public BookAiService(HttpClient http, IOptions<ExternalApisOptions> options, ILogger<BookAiService> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> GetBookRecommendationsAsync(IEnumerable<Book> userBooks, CancellationToken cancellationToken = default)
        {
            var apiKey = _options.GeminiApiKey?.Trim();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return "AI препоръките не са налични (липсва Gemini API ключ в конфигурацията).";
            }

            var model = string.IsNullOrWhiteSpace(_options.GeminiModel) ? "gemini-3-flash-preview" : _options.GeminiModel.Trim();
            var booksList = userBooks?.ToList() ?? new List<Book>();

            string prompt;
            if (booksList.Any())
            {
                var bookTitles = string.Join(", ", booksList.Select(b => $"\"{b.Title}\" от {b.Author} ({b.Genre})"));
                prompt = $"Потребителят има следните книги на своя личен книжен рафт: {bookTitles}. " +
                         $"На базата на тези жанрове и автори, препоръчай му 3 нови интересни книги, които да прочете. " +
                         $"Върни кратки, вдъхновяващи препоръки на български език с формат: Заглавие - Автор: Кратко описание защо ще му харесат.";
            }
            else
            {
                prompt = "Потребителят все още няма добавени книги на своя книжен рафт. " +
                         $"Моля, предложи му 3 актуални световни и български бестселъра към момента на български език " +
                         $"във формат: Заглавие - Автор: Кратко описание.";
            }

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7 // По-висока температура за творчески препоръки
                }
            };

            var serializedPayload = JsonSerializer.Serialize(requestBody, JsonOptions);
            var endpoint = $"v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(apiKey)}";

            try
            {
                using var content = new StringContent(serializedPayload, Encoding.UTF8, "application/json");
                using var response = await _http.PostAsync(endpoint, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return "В момента ИИ услугите са натоварени. Опитайте по-късно.";
                }

                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseText);

                var text = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return text ?? "Няма получени препоръки от ИИ.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Грешка при връзка с Gemini за книжни препоръки.");
                return "Възникна мрежова грешка при връзка с изкуствения интелект.";
            }
        }
    }
}
