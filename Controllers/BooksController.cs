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
        private readonly IDatabase db; //достъп до базата
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
        //филтриране на книгите по заглавие и жанр
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

            return View(books); //зарежда филтрираните книги
        }

        // INSERT - GET
        public IActionResult Create()
        {
            if (GetCurrentUserId() == null) return RedirectToAction("Login", "Account");

            ViewData["Genres"] = GenreHelper.GetGenreSelectList();
            return View();
        }

        // INSERT - POST
        //обработва изпратената форма за нова книга
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

                db.AddBook(book);//записва новата книга в базата данни
                return RedirectToAction(nameof(Index));
            }

            ViewData["Genres"] = GenreHelper.GetGenreSelectList(book.Genre);
            return View(book);
        }

        // разпознаване на книга от снимка на корицата; попълва формата за преглед
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtractFromImage(IFormFile? imageFile, CancellationToken cancellationToken)
        {
            if (GetCurrentUserId() == null) return RedirectToAction("Login", "Account");

            //проверява спецификации за снимката
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

            // Подава на ИИ списъка с жанровете от сайта, за да избере един от тях
            var genres = GenreHelper.GetGenreSelectList()
                .Select(g => g.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();

            //анализ от ИИ
            var result = await aiService.RecognizeCoverAsync(ms.ToArray(), mimeType, genres, cancellationToken);

            if (!result.IsSuccess)
            {
                ViewData["AiError"] = result.Error;
                return View("Create", new Book()); 
            }

            ModelState.Clear();

            var info = result.Data!;
            //нова книга с даннмите които са се генерирали
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

            return View("Create", book); //връща формата попълнена
        }

        // EDIT - GET
        //зарежда формата за редакция
        public IActionResult Edit(int id)
        {
            if (GetCurrentUserId() == null) return RedirectToAction("Login", "Account");

            var book = db.GetBookById(id);
            if (book == null || book.UserId != GetCurrentUserId()) return NotFound();

            ViewData["Genres"] = GenreHelper.GetGenreSelectList(book.Genre);
            return View(book);
        }

        // EDIT - POST
        //записва промените по съществуваща книга
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
                    string uploadsFolder = Path.Combine(env.WebRootPath, "uploads"); //създава папката ако я няма
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + pdfFile.FileName; //добавя неповтъоримо име на файла
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

                db.UpdateBook(book); //актуализира данните
                return RedirectToAction(nameof(Index));
            }

            ViewData["Genres"] = GenreHelper.GetGenreSelectList(book.Genre);

            return View(book);
        }

        // DELETE - GET
        //зарежда "потвърждение" преди изтриване
        public IActionResult Delete(int id)
        {
            if (GetCurrentUserId() == null) return RedirectToAction("Login", "Account");

            var book = db.GetBookById(id);
            if (book == null || book.UserId != GetCurrentUserId()) return NotFound();

            return View(book);
        }

        // DELETE - POST
        //изтрива книгата от базата след потвърждение
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
        //страницата с пълната информация за книгата 
        public IActionResult Details(int id)
        {
            if (GetCurrentUserId() == null) return RedirectToAction("Login", "Account");

            var book = db.GetBookById(id);
            if (book == null || book.UserId != GetCurrentUserId()) return NotFound();

            return View(book);
        }

        // DELETE PDF - POST
        //изтрива само прикачения към книгата файл както и пътя му от базата
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