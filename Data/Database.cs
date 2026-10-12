using BookLibrary.Models;

namespace BookLibrary.Data
{
    public class Database : IDatabase
    {
        private readonly List<Book> books = new();
        private int nextId = 1;


        // Insert
        public void AddBook(Book book)
        {
            book.Id = nextId++;
            books.Add(book);
        }

        // Select with Filter
        public List<Book> GetAllBooks(string? search = null, string? genre = null)
        {
            var query = books.AsQueryable();

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
            return books.FirstOrDefault(b => b.Id == id);
        }
    }
}
