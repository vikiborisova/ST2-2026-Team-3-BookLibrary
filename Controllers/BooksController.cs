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
        public async Task<IActionResult> Create(Book book, IFormFile? pdfFile)
        {
            if (ModelState.IsValid)
            {
                if (pdfFile != null && pdfFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(env.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + pdfFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await pdfFile.CopyToAsync(fileStream);
                    }

                    book.PdfPath = "/uploads/" + uniqueFileName;
                }

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

            var existingBook = db.GetBookById(id);
            if (existingBook == null) return NotFound();

            if (ModelState.IsValid)
            {
                if (pdfFile != null && pdfFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(env.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + pdfFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await pdfFile.CopyToAsync(fileStream);
                    }


                    book.PdfPath = "/uploads/" + uniqueFileName;
                }
                else
                {
                    book.PdfPath = existingBook.PdfPath;
                }

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
