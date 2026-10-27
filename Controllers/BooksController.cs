using Microsoft.AspNetCore.Mvc;
using BookLibrary.Data;
using BookLibrary.Models;
using BookLibrary.Helpers;
using BookLibrary.Services;
using System.Threading;

namespace BookLibrary.Controllers
{
    public class BooksController : Controller
    {
        private readonly IDatabase db;
        private readonly IWebHostEnvironment env;
        private readonly IBookAiService ai;
        public BooksController(IDatabase db, IBookAiService ai, IWebHostEnvironment env)
        {
            this.db = db;
            this.env = env;
            this.ai = ai;
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        // SELECT WITH FILTER
        public async Task<IActionResult> IndexAsync(string? search, string? genre, CancellationToken cancellationToken)
        {
            int? userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var books = db.GetAllBooks(userId.Value, search, genre);
            string aiRecommendations = await ai.GetBookRecommendationsAsync(books, cancellationToken);

            ViewData["CurrentSearch"] = search;
            ViewData["CurrentGenre"] = genre;
            ViewData["AiRecommendations"] = aiRecommendations;

            ViewData["Genres"] = GenreHelper.GetGenreSelectList(genre);
            return View(books);
        }

        // INSERT - GET
        public IActionResult Create()
        {
            if (GetCurrentUserId() == null) return RedirectToAction("Login", "Account");
            
            ViewData["Genres"] = GenreHelper.GetGenreSelectList();
            return View();
        }

        // INSERT - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book book, IFormFile? pdfFile)
        {
            int? userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

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
                book.UserId = userId.Value;

                db.AddBook(book);
                return RedirectToAction(nameof(Index));
            }

            ViewData["Genres"] = GenreHelper.GetGenreSelectList(book.Genre);
            return View(book);
        }

        // EDIT - GET
        public IActionResult Edit(int id)
        {
            if (GetCurrentUserId() == null) return RedirectToAction("Login", "Account");

            var book = db.GetBookById(id);
            if (book == null || book.UserId != GetCurrentUserId()) return NotFound();

            ViewData["Genres"] = GenreHelper.GetGenreSelectList(book.Genre);
            return View(book);
        }
        // EDIT - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Book book, IFormFile? pdfFile)
        {
            int? userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

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
                book.UserId = existingBook.UserId;

                db.UpdateBook(book);
                return RedirectToAction(nameof(Index));
            }

            ViewData["Genres"] = GenreHelper.GetGenreSelectList(book.Genre);
            return View(book);

        }

        // DELETE - GET
        public IActionResult Delete(int id)
        {
            if (GetCurrentUserId() == null) return RedirectToAction("Login", "Account");

            var book = db.GetBookById(id);
            if (book == null || book.UserId != GetCurrentUserId()) return NotFound();

            return View(book);
        }

        // DELETE - POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            int? userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var book = db.GetBookById(id);
            if (book != null && book.UserId == userId.Value)
            {
                db.DeleteBook(id);
            }

            return RedirectToAction(nameof(Index));
        }

        // DETAILS
        public IActionResult Details(int id)
        {
            if (GetCurrentUserId() == null) return RedirectToAction("Login", "Account");

            var book = db.GetBookById(id);
            if (book == null || book.UserId != GetCurrentUserId()) return NotFound();

            return View(book);
        }

        // DELETE PDF - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePdf(int id)
        {
            int? userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var book = db.GetBookById(id);
            if (book == null || book.UserId != userId.Value) return NotFound();

            if (!string.IsNullOrEmpty(book.PdfPath))
            {
                // Изтриваме самия файл от wwwroot/uploads
                string fileName = Path.GetFileName(book.PdfPath);
                string filePath = Path.Combine(env.WebRootPath, "uploads", fileName);

                try
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                catch (IOException)
                {
                    // Файлът може да е отворен от браузъра, махаме го от базата и без това
                }

                // Махаме пътя от базата
                book.PdfPath = string.Empty;
                db.UpdateBook(book);
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
