using BookLibrary.Services;

namespace BookLibrary.Configuration
{
    //Настройки за локалния AI
    public class LocalAiOptions
    {
        public const string SectionName = "LocalAi";

        public bool Enabled { get; set; } = true; //ползва ли се

        public string? BaseUrl { get; set; }

        public string? Model { get; set; }

        public string? ApiKey { get; set; }

        public bool SupportsVision { get; set; } = true; //разпознаване на изображения

        public int TimeoutSeconds { get; set; } = 60;

        public bool UseJsonResponseFormat { get; set; }

        public CircuitBreakerOptions CircuitBreaker { get; set; } = new(); //предпазен механизъм; спира заявки ако даде грешка
    }
}
