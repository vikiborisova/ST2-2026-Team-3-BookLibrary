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
        private const int RecommendationCount = 6; //брой препоръки по ИИ

        private readonly ILogger<HomeController> logger;
        private readonly IDatabase db;
        private readonly IBookAiService ai;
        private readonly IMemoryCache cache;

        public HomeController(ILogger<HomeController> logger, IDatabase db, IBookAiService ai, IMemoryCache cache)
        {
            this.logger = logger;
            this.db = db;
            this.ai = ai;
            this.cache = cache;
        }


        public IActionResult Index()
        {
            return View();
        }

        
        [HttpGet]
        //метод за генериранед и кеширане на препоръки 
        public async Task<IActionResult> Recommendations(CancellationToken cancellationToken)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var books = userId is null
                ? new List<Book>()
                : db.GetAllBooks(userId.Value, null, null).ToList();

           
            var key = userId is null
                ? "recs:popular" //ключ гост потребител
                : $"recs:user:{userId}:{books.Count}:{books.Sum(b => (long)b.Id)}"; //ключ регистриран потребител

            if (!cache.TryGetValue(key, out IReadOnlyList<BookRecommendation>? recs) || recs is null) //дали в паметта има запазени препоръки
            {
                var result = await ai.GetBookRecommendationsAsync(books, RecommendationCount, cancellationToken);
                recs = result.Data ?? Array.Empty<BookRecommendation>();

                
                cache.Set(key, recs, recs.Count > 0 ? TimeSpan.FromHours(12) : TimeSpan.FromMinutes(1));//ако има намерени препоръки ги пази 12 часа
            }

            return Json(new { personalized = books.Count > 0, items = recs });
        }

        //страница за поверителност на данните
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

        //при грешка в приложението
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}