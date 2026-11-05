using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookLibrary.Helpers
{
    public static class GenreHelper
    {
        public static List<string> GetGenres()
        {
            return new List<string>
            {
                "Класически роман",
                "Фентъзи",
                "Научна фантастика",
                "Антиутопия",
                "Трилър",
                "Криминален",
                "Исторически",
                "Биография",
                "Научно-популярен",
                "Друг",
                "Класическа драма",
                "Психологически роман",
                "Философска приказка",
                "Българска фантастика",
                "Приключенски",
                "Исторически роман",
                "Психологически трилър",
                "Детективски роман",
                "Автобиографичен",
                "Пътешествие",
                "Научно-популярна",
                "Мемоари",
                "Съвременна проза",
                "Фантастика",
                "Програмиране",
                "Киберпънк",
                "Романтика",
                "Сатира",
                "Пост-апокалипсис"
            };
        }

        public static SelectList GetGenreSelectList(string? selectedGenre = null)
        {
            return new SelectList(GetGenres(), selectedGenre);
        }
    }
}
