using BookLibrary.Models;

namespace BookLibrary.Services;

//какви възможности трябва задължително да притежава всеки AI уред или услуга в приложението
public interface IBookAiService
{
    //генериране на книжни препоръки
    Task<AiResult<IReadOnlyList<BookRecommendation>>> GetBookRecommendationsAsync(
        IEnumerable<Book> userBooks,
        int count = 6,//брой препоръки
        CancellationToken cancellationToken = default);

    //разпознаване на книга по снимка на корицата
    Task<AiResult<BookCoverInfo>> RecognizeCoverAsync(
        byte[] imageBytes,
        string mimeType, //формат
        IReadOnlyList<string> genres,//по коректно
        CancellationToken cancellationToken = default);
}
