using BookLibrary.Models;

namespace BookLibrary.Data
{
    public interface IDatabase
    {
        List<Book> GetAllBooks(string? search = null, string? genre = null);
        Book? GetBookById(int id);
        void AddBook(Book book);
    }
}
