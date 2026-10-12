using Microsoft.AspNetCore.Mvc;
using BookLibrary.Data;
using BookLibrary.Models;
using BookLibrary.Helpers;

namespace BookLibrary.Controllers
{
    public class BooksController : Controller
    {
        private readonly IDatabase db;
        private readonly IWebHostEnvironment env;
        public BooksController(IDatabase db, IWebHostEnvironment env)
        {
            this.db = db;
            this.env = env;
        }

        // SELECT WITH FILTER
        public IActionResult Index(string? search, string? genre)
        {
            var books = db.GetAllBooks(search, genre);
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentGenre"] = genre;

            ViewData["Genres"] = GenreHelper.GetGenreSelectList(genre);
            return View(books);
        }

        // INSERT - GET
        public IActionResult Create()
        {
            ViewData["Genres"] = GenreHelper.GetGenreSelectList();
            return View();
        }

        // INSERT - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Book book)
        {
            if (ModelState.IsValid)
            {
                db.AddBook(book);
                return RedirectToAction(nameof(Index));
            }
            ViewData["Genres"] = GenreHelper.GetGenreSelectList(book.Genre);
            return View(book);
        }
    }
}
