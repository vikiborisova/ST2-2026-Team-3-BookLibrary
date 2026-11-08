using Microsoft.AspNetCore.Mvc;
using BookLibrary.Data;
using BookLibrary.Models;
using BookLibrary.Helpers;
using BookLibrary.Services;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System;

namespace BookLibrary.Controllers
{
    public class BooksController : Controller
    {
        private readonly IDatabase db;
        private readonly IWebHostEnvironment env;
        private readonly IBookAiService aiService;

        public BooksController(IDatabase db, IWebHostEnvironment env, IBookAiService aiService)
        {
            this.db = db;
            this.env = env;
            this.aiService = aiService;
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        // SELECT WITH FILTER
        public IActionResult Index(string? search, string? genre)
        {
            int? userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var books = db.GetAllBooks(userId.Value, search, genre);

            ViewData["CurrentSearch"] = search;
            ViewData["CurrentGenre"] = genre;
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

        // AI: разпознаване на книга от снимка на корицата -> попълва формата за преглед
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtractFromImage(IFormFile? imageFile, CancellationToken cancellationToken)
        {
            if (GetCurrentUserId() == null) return RedirectToAction("Login", "Account");

            const long maxSize = 10 * 1024 * 1024; // 10 MB
            var allowed = new[] { "image/jpeg", "image/png", "image/webp" };

            ViewData["Genres"] = GenreHelper.GetGenreSelectList();

            if (imageFile == null || imageFile.Length == 0)
            {
                ViewData["AiError"] = "Моля, изберете снимка на корицата.";
                return View("Create", new Book());
            }
            if (imageFile.Length > maxSize)
            {
                ViewData["AiError"] = "Снимката не може да е по-голяма от 10 MB.";
                return View("Create", new Book());
            }

            var mimeType = (imageFile.ContentType ?? string.Empty).ToLowerInvariant();
            if (!allowed.Contains(mimeType))
            {
                ViewData["AiError"] = "Разрешени са само JPEG, PNG и WebP снимки.";
                return View("Create", new Book());
            }

            using var ms = new MemoryStream();
            await imageFile.CopyToAsync(ms, cancellationToken);

            // Подаваме на ИИ списъка с жанровете от сайта, за да избере един от тях
            var genres = GenreHelper.GetGenres()
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();

            var result = await aiService.RecognizeCoverAsync(ms.ToArray(), mimeType, genres, cancellationToken);

            if (!result.IsSuccess)
            {
                ViewData["AiError"] = result.Error;
                return View("Create", new Book());
            }

            // Без това asp-for показва празни стойности от ModelState
            ModelState.Clear();

            var info = result.Data!;
            var book = new Book
            {
                Title = info.Title,
                Author = info.Author,
                Genre = info.Genre ?? string.Empty,
                Description = info.Description ?? string.Empty,
                Status = "Ще чета"
            };
            if (info.Year.HasValue) book.Year = info.Year.Value;

            ViewData["Genres"] = GenreHelper.GetGenreSelectList(book.Genre);
            ViewData["AiNotice"] = result.Notice;
            ViewData["AiProvider"] = result.Provider;

            return View("Create", book);
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