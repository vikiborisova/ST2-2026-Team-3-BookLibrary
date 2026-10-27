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

        // Извиква се от каросела на началната страница (fetch), за да не се бави зареждането на страницата
        [HttpGet]
        public async Task<IActionResult> Recommendations(CancellationToken cancellationToken)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var books = userId is null
                ? new List<Book>()
                : _db.GetAllBooks(userId.Value, null, null).ToList();

            // Ключът се променя, когато потребителят добави/изтрие книга -> препоръките се обновяват
            var key = userId is null
                ? "recs:popular"
                : $"recs:user:{userId}:{books.Count}:{books.Sum(b => (long)b.Id)}";

            if (!_cache.TryGetValue(key, out IReadOnlyList<BookRecommendation>? recs) || recs is null)
            {
                recs = await _ai.GetBookRecommendationsAsync(books, RecommendationCount, cancellationToken);

                // успех: 12 часа; неуспех: само 1 минута, за да не товарим Gemini при всяко зареждане
                _cache.Set(key, recs, recs.Count > 0 ? TimeSpan.FromHours(12) : TimeSpan.FromMinutes(1));
            }

            return Json(new { personalized = books.Count > 0, items = recs });
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