using BookLibrary.Models;

namespace BookLibrary.Services;

internal static class AiPrompts
{
    public const string CoverVisionSystem = """
        Ти разпознаваш книги от снимка на корица.
        Прочети заглавието, автора, жанра (ако е наличен), годината на издаване (ако е видима) и дай кратко описание.
        Върни САМО валиден JSON обект със следните ключове: 
        {"title": "...", "author": "...", "genre": "...", "year": 1999, "description": "..."}
        Правила:
        1. Запази езика и азбуката, с които са изписани заглавието и авторът на корицата.
        2. За genre и description използвай български език.
        3. Ако дадено поле не може да бъде установено от корицата, върни празен низ "" за текстовите полета или null за годината (year).
        4. Не измисляй фалшиви данни.
        """;

    public const string CoverVisionUser = "Прочети детайлите, заглавието, автора, евентуалната година и описание от корицата на тази книга.";

    private const string RecommendationJsonInstructions =
        "Върни САМО валиден JSON масив от обекти (без никакъв Markdown или допълнителен текст извън масива) с полета: " +
        "\"title\" (заглавие на книгата), \"author\" (автор), \"genre\" (жанр) и " +
        "\"description\" (2-3 изречения на български език защо книгата ще му хареса).";

    public static string Recommendations(IReadOnlyList<Book> userBooks, int count)
    {
        if (userBooks.Count > 0)
        {
            var bookTitles = string.Join("; ", userBooks.Select(b => $"\"{b.Title}\" от {b.Author} ({b.Genre})"));
            return $"Потребителят има следните книги на своя личен рафт: {bookTitles}. " +
                   $"На базата на тези жанрове и автори препоръчай точно {count} нови книги, " +
                   $"които НЕ са сред изброените. " + RecommendationJsonInstructions;
        }

        return $"Потребителят все още няма книги на своя рафт. Предложи точно {count} разнообразни " +
               $"популярни книги към момента - световни и български бестселъри. " + RecommendationJsonInstructions;
    }

    public static string CoverDetails(string title, string author, IReadOnlyList<string> genres, bool webSearch) =>
        (webSearch ? "Намери в интернет информация за книгата" : "Дай информация за книгата") +
        $" „{title}“" +
        (string.IsNullOrWhiteSpace(author) ? "" : $" от {author}") + ".\n" +
        "Върни САМО валиден JSON обект, без никакъв Markdown (като ```json ... ```): " +
        "{\"title\": \"...\", \"author\": \"...\", \"genre\": \"...\", \"year\": 0000, \"description\": \"...\"}\n" +
        "Правила:\n" +
        "1. title и author са официалното изписване на езика на книгата.\n" +
        "2. genre е точно един от следния списък (ако е приложимо): " + (genres.Count > 0 ? string.Join(", ", genres) : "общ жанр") + ". Ако нито един не пасва точно, избери най-близкия логичен.\n" +
        "3. year е годината на първото издание или изданието като цяло като цяло число (например 2020), или null ако съвсем липсва информация.\n" +
        "4. description са 2 до 3 изречения на български език, обобщаващи съдържанието без спойлери.\n" +
        "5. Бъди максимално прецизен и избягвай празни полета, ако информацията е общоизвестна.";
}