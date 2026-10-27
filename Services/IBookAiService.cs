using BookLibrary.Models;

namespace BookLibrary.Services
{
    public interface IBookAiService
    {
        /// <summary>
        /// Връща до <paramref name="count"/> препоръки. Празен списък = услугата не е налична.
        /// Ако потребителят няма книги, връща популярни книги.
        /// </summary>
        Task<IReadOnlyList<BookRecommendation>> GetBookRecommendationsAsync(
            IEnumerable<Book> userBooks,
            int count = 6,
            CancellationToken cancellationToken = default);
    }
}
