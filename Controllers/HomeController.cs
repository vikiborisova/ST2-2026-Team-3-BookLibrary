using System.Diagnostics;
using BookLibrary.Data;
using BookLibrary.Models;
using BookLibrary.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BookLibrary.Controllers
{
    public class HomeController : Controller
    {
        private const int RecommendationCount = 6;

        private readonly ILogger<HomeController> _logger;
        private readonly IDatabase _db;
        private readonly IBookAiService _ai;
        private readonly IMemoryCache _cache;

        public HomeController(ILogger<HomeController> logger, IDatabase db, IBookAiService ai, IMemoryCache cache)
        {
            _logger = logger;
            _db = db;
            _ai = ai;
            _cache = cache;
        }

        public IActionResult Index()
        {
            return View();
        }

        
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> Recommendations(CancellationToken cancellationToken)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                var books = userId is null
                    ? new List<Book>()
                    : _db.GetAllBooks(userId.Value, null, null).ToList();

                var key = userId is null
                    ? "recs:popular"
                    : $"recs:user:{userId}:{books.Count}:{books.Sum(b => (long)b.Id)}";

                if (!_cache.TryGetValue(key, out IReadOnlyList<BookRecommendation>? recs) || recs is null)
                {
                    var result = await _ai.GetBookRecommendationsAsync(books, RecommendationCount, cancellationToken);

                    // Проверяваме дали услугата е върнала успешен резултат
                    recs = (result != null && result.Data != null) ? result.Data : Array.Empty<BookRecommendation>();

                    _cache.Set(key, recs, recs.Count > 0 ? TimeSpan.FromHours(12) : TimeSpan.FromMinutes(1));
                }

                return Json(new { personalized = books.Count > 0, items = recs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Грешка при зареждане на препоръките.");
                // Връщаме празен списък с грешка към фронтенда, за да не гръмне страницата
                return Json(new { personalized = false, items = Array.Empty<BookRecommendation>(), error = ex.Message });
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}