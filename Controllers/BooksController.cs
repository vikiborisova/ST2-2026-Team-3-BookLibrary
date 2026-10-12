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

        // EDIT - GET
        public IActionResult Edit(int id)
        {
            var book = db.GetBookById(id);
            if (book == null) return NotFound();

            ViewData["Genres"] = GenreHelper.GetGenreSelectList(book.Genre);
            return View(book);
        }
        // EDIT - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Book book, IFormFile? pdfFile)
        {
            if (id != book.Id) return NotFound();

            if (ModelState.IsValid)
            {

                db.UpdateBook(book);
                return RedirectToAction(nameof(Index));
            }

            ViewData["Genres"] = GenreHelper.GetGenreSelectList(book.Genre);
            return View(book);

        }

        // DELETE - GET
        public IActionResult Delete(int id)
        {
            var book = db.GetBookById(id);
            if (book == null) return NotFound();
            return View(book);
        }

        // DELETE - POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            db.DeleteBook(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
