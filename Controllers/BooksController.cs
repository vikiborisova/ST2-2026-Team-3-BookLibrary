using Microsoft.AspNetCore.Mvc;
using BookLibrary.Data;
using BookLibrary.Models;
using BookLibrary.Helpers;
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

        public BooksController(IDatabase db, IWebHostEnvironment env)
        {
            this.db = db;
            this.env = env;
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

            // ТОВА е редът, който липсваше и причиняваше грешката:
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
    }
}