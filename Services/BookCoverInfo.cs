namespace BookLibrary.Services;

public sealed class BookCoverInfo
{
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string? Genre { get; init; }
    public int? Year { get; init; }
    public string? Description { get; init; }
}
