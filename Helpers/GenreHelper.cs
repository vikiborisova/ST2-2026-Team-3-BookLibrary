using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookLibrary.Helpers
{
    public static class GenreHelper
    {
        public static List<string> GetGenres()
        {
            return new List<string>
            {
                "Роман",
                "Фентъзи",
                "Научна фантастика",
                "Антиутопия",
                "Трилър",
                "Криминален",
                "Исторически",
                "Биография",
                "Научно-популярен",
                "Друг"
            };
        }

        public static SelectList GetGenreSelectList(string? selectedGenre = null)
        {
            return new SelectList(GetGenres(), selectedGenre);
        }
    }
}
