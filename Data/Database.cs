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

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b => b.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                         b.Author.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                         b.Year.ToString().Equals(search) ||
                                         b.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(genre))
            {
                query = query.Where(b => b.Genre.Contains(genre, StringComparison.OrdinalIgnoreCase));
            }

            return query.ToList();
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
