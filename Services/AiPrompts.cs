using BookLibrary.Models;

namespace BookLibrary.Services;

internal static class AiPrompts
{
    public const string CoverVisionSystem = """
        Ти разпознаваш книги от снимка на корица.
        Прочети заглавието и автора, които са написани на корицата.
        Върни САМО валиден JSON обект: {"title": "...", "author": "..."}
        Правила:
        1. Запази езика и азбуката, с които са изписани на корицата.
        2. Ако не се чете заглавие или автор, върни за него празен низ "".
        3. Не измисляй и не допълвай липсващи данни.
        """;

    public const string CoverVisionUser = "Прочети заглавието и автора от корицата на тази книга.";

    private const string RecommendationJsonInstructions =
        "Върни САМО JSON масив от обекти с полета: " +
        "\"title\" (заглавие на български), \"author\" (автор) и " +
        "\"description\" (2-3 изречения на български език защо книгата ще му хареса).";

    public static string Recommendations(IReadOnlyList<Book> userBooks, int count)
    {
        if (userBooks.Count > 0)
        {
            var bookTitles = string.Join("; ", userBooks.Select(b => $"\"{b.Title}\" от {b.Author} ({b.Genre})"));
            return $"Потребителят има следните книги на своя личен рафт: {bookTitles}. " +
                   $"На базата на тези жанрове и автори препоръчай точно {count} нови книги, " +
                   "които НЕ са сред изброените. " + RecommendationJsonInstructions;
        }

        return $"Потребителят все още няма книги на своя рафт. Предложи точно {count} разнообразни " +
               "популярни книги към момента - световни и български бестселъри. " + RecommendationJsonInstructions;
    }

    public static string CoverDetails(string title, string author, IReadOnlyList<string> genres, bool webSearch) =>
        (webSearch ? "Намери в интернет информация за книгата" : "Дай информация за книгата") +
        $" „{title}“" +
        (string.IsNullOrWhiteSpace(author) ? "" : $" от {author}") + ".\n" +
        "Върни САМО JSON обект, без Markdown: " +
        "{\"title\": \"...\", \"author\": \"...\", \"genre\": \"...\", \"year\": 1999, \"description\": \"...\"}\n" +
        "Правила:\n" +
        "1. title и author са официалното изписване, на езика на корицата.\n" +
        "2. genre е точно един от списъка: " + string.Join(", ", genres) + ". Ако нито един не пасва, върни \"\".\n" +
        "3. year е годината на първото издание като число, или null.\n" +
        "4. description са 2 до 3 изречения на български, без спойлери.\n" +
        "5. Ако не си сигурен, остави празен низ. Не измисляй.";
}
