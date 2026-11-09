using BookLibrary.Models;

namespace BookLibrary.Data
{
    //какви методи трябва задължително да притежава всеки клас, работещ с базата
    public interface IDatabase
    {
        // Методи за потребители
        User? RegisterUser(string username, string password); //регистрация
        User? AuthenticateUser(string username, string password); //вход


        // Mетоди за книги
        List<Book> GetAllBooks(int userId, string? search = null, string? genre = null); //списък с книги и филтри
        Book? GetBookById(int id); //книга по id 
        void AddBook(Book book); //добавяне на книга

        void UpdateBook(Book book); //редактиране на книга
        void DeleteBook(int id); //изтриване на книга
    }
}
