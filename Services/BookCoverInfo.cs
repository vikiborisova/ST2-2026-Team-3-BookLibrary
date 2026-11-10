namespace BookLibrary.Services;

//за съхранение на разпознатата от ИИ информация за дадена книга преди тя да бъде записвана в базата данни или заредена във формата за създаване
public sealed class BookCoverInfo
{
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string? Genre { get; init; }
    public int? Year { get; init; }
    public string? Description { get; init; }
}
