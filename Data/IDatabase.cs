using BookLibrary.Models;

namespace BookLibrary.Data
{
    public interface IDatabase
    {
        // Методи за потребители
        User? RegisterUser(string username, string password);
        User? AuthenticateUser(string username, string password);


        // Mетоди за книги
        List<Book> GetAllBooks(int userId, string? search = null, string? genre = null);
        Book? GetBookById(int id);
        void AddBook(Book book);

        void UpdateBook(Book book);
        void DeleteBook(int id);
    }
}
