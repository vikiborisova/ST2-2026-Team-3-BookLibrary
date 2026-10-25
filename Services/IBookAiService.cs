using BookLibrary.Models;

namespace BookLibrary.Services
{
    public interface IBookAiService
    {
        Task<string> GetBookRecommendationsAsync(IEnumerable<Book> userBooks, CancellationToken cancellationToken = default);
    }
}
