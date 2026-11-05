namespace BookLibrary.Models
{
    // Модел за страницата с грешка (Error.cshtml). Генерира се автоматично от ASP.NET.
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        // true само ако има номер, за да не се показва празен ред
    }
}
