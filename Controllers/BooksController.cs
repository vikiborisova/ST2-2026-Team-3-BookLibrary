using Microsoft.AspNetCore.Mvc;
using BookLibrary.Data;
using BookLibrary.Models;

namespace BookLibrary.Controllers
{
    public class BooksController : Controller
    {
        private readonly IDatabase db;
        public BooksController(IDatabase db)
        {
            this.db = db;
        }

        // SELECT WITH FILTER
        public IActionResult Index(string? search, string? genre)
        {
            var books = db.GetAllBooks(search, genre);
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentGenre"] = genre;
            return View(books);
        }

        // INSERT - GET
        public IActionResult Create()
        {
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
            return View(book);
        }
    }
}
