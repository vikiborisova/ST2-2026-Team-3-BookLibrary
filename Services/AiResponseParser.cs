using System.Text.Json;
using BookLibrary.Models;

namespace BookLibrary.Services;

internal static class AiResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };//няма да правят разлика между главни и малки букви

    public static JsonElement ParseObject(string text) //взема текст от ИИ и го превръща в JSON елемент
    {
        var first = text.IndexOf('{');
        var last = text.LastIndexOf('}');
        var json = first >= 0 && last > first ? text[first..(last + 1)] : "{}";
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    //парсира препоръките; проверява дали са масив или обект
    public static IReadOnlyList<BookRecommendation> ParseRecommendations(string text, int count)
    {
        text = text.Trim();
        var arrayStart = text.IndexOf('[');
        var objectStart = text.IndexOf('{');

        List<BookRecommendation>? items;
        if (arrayStart >= 0 && (objectStart < 0 || arrayStart < objectStart))
        {
            var end = text.LastIndexOf(']');
            if (end <= arrayStart) throw new JsonException("Unterminated JSON array.");
            items = JsonSerializer.Deserialize<List<BookRecommendation>>(text[arrayStart..(end + 1)], JsonOptions);
        }
        else
        {
            var root = ParseObject(text);
            var array = root.ValueKind == JsonValueKind.Object
                ? root.EnumerateObject().Select(p => p.Value).FirstOrDefault(v => v.ValueKind == JsonValueKind.Array)
                : default;
            items = array.ValueKind == JsonValueKind.Array
                ? array.Deserialize<List<BookRecommendation>>(JsonOptions)
                : null;
        }

        return (items ?? new List<BookRecommendation>())
            .Where(r => !string.IsNullOrWhiteSpace(r.Title))
            .Select(r => new BookRecommendation
            {
                Title = r.Title.Trim(),
                Author = r.Author?.Trim() ?? "",
                Description = r.Description?.Trim() ?? ""
            })
            .Take(count)
            .ToList();
    }

    //преобразува JSON от корицата в структуриран обект BookCoverInfo
    public static BookCoverInfo ToCoverInfo(JsonElement found, string title, string author, IReadOnlyList<string> genres)
    {
        var foundGenre = GetString(found, "genre");
        var matchedGenre = genres.FirstOrDefault(g => string.Equals(g, foundGenre, StringComparison.OrdinalIgnoreCase));

        var description = GetString(found, "description");
        if (description.Length > 800) description = description[..800];

        return new BookCoverInfo
        {
            Title = FirstNonEmpty(GetString(found, "title"), title),
            Author = FirstNonEmpty(GetString(found, "author"), author),
            Genre = matchedGenre,
            Year = GetYear(found),
            Description = string.IsNullOrWhiteSpace(description) ? null : description
        };
    }
    //взима текстова стойност по дадено име на свойство от JSON елемент, като връща празен низ при липса на такова
    public static string GetString(JsonElement el, string name) =>
        el.ValueKind == JsonValueKind.Object &&
        el.TryGetProperty(name, out var v) &&
        v.ValueKind == JsonValueKind.String
            ? (v.GetString() ?? string.Empty).Trim()
            : string.Empty;

    //извлича годината от JSON и проверява диапазона
    private static int? GetYear(JsonElement el)
    {
        if (el.ValueKind != JsonValueKind.Object || !el.TryGetProperty("year", out var v)) return null;

        int year;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out year)) { }
        else if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out year)) { }
        else return null;

        return year is >= 1000 and <= 2100 ? year : null;
    }

    //връща първия непразен низ между две стойности
    private static string FirstNonEmpty(string a, string b) => string.IsNullOrWhiteSpace(a) ? b : a;
}
