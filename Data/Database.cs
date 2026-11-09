using BookLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace BookLibrary.Data
{
    public class Database : IDatabase
    {

        private readonly AppDbContext context;

        public Database(AppDbContext context)
        {
            this.context = context;
        }


        public User? RegisterUser(string username, string password)
        {
            if (context.Users.Any(u => u.Username.ToLower() == username.ToLower()))
            {
                return null;
            }

            var user = new User { Username = username, Password = password };
            context.Users.Add(user);
                   context.SaveChanges();
            return user;
        }

        public User? AuthenticateUser(string username, string password)
        {
            return context.Users.FirstOrDefault(u => u.Username.ToLower() == username.ToLower() && u.Password == password);
        }


        public void AddBook(Book book)
        {
            context.Books.Add(book);
            context.SaveChanges();
        }

        public void DeleteBook(int id)
        {
            var book = GetBookById(id);
            if (book != null)
            {
                context.Books.Remove(book);
                context.SaveChanges();
            }
        }



        // Select with Filter
        public List<Book> GetAllBooks(int userId, string? search = null, string? genre = null)
        {
            var query = context.Books.Where(b => b.UserId == userId).AsQueryable();

            if (!string.IsNullOrWhiteSpace(genre))
            {
                query = query.Where(b => b.Genre.Contains(genre));
            }

            var books = query.ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                var isYear = int.TryParse(term, out var searchYear);
                const StringComparison comp = StringComparison.OrdinalIgnoreCase;

                books = books.Where(b =>
                    (b.Title?.Contains(term, comp)  ?? false) ||
                    (b.Author?.Contains(term, comp) ?? false) ||
                    (isYear && b.Year == searchYear) ||
                    (b.Description?.Contains(term, comp) ?? false))
                    .ToList();
            }

            return books;
        }

        public Book? GetBookById(int id)
        {
            return context.Books.FirstOrDefault(b => b.Id == id);
        }

        public void UpdateBook(Book updatedBook)
        {
            var existingBook = GetBookById(updatedBook.Id);
            if (existingBook != null)
            {
                existingBook.Title = updatedBook.Title;
                existingBook.Author = updatedBook.Author;
                existingBook.Genre = updatedBook.Genre;
                existingBook.Year = updatedBook.Year;
                existingBook.Description = updatedBook.Description;
                existingBook.Status = updatedBook.Status;
                existingBook.PdfPath = updatedBook.PdfPath;

                context.SaveChanges();
            }
        }
    }
}
