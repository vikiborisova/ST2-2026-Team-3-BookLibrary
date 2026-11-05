using BookLibrary.Models;

namespace BookLibrary.Services;

public interface IBookAiService
{
    Task<AiResult<IReadOnlyList<BookRecommendation>>> GetBookRecommendationsAsync(
        IEnumerable<Book> userBooks,
        int count = 6,
        CancellationToken cancellationToken = default);

    Task<AiResult<BookCoverInfo>> RecognizeCoverAsync(
        byte[] imageBytes,
        string mimeType,
        IReadOnlyList<string> genres,
        CancellationToken cancellationToken = default);
}
