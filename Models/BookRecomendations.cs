namespace BookLibrary.Models
{
    // модел за една препоръчана книга (от AI);
    // не се записва в базата, ползва се само за показване в карусела на началната страница
    public class BookRecommendation
    {
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string Description { get; set; } = "";
    }
}