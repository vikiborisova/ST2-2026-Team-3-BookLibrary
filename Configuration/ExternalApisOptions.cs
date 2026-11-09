using System.ComponentModel;

namespace BookLibrary.Configuration
{
    //модел за настройките, свързани с външни уеб услуги и API интеграции
    public class ExternalApisOptions
    {
        public const string SectionName = "ExternalApis";

        public string? GeminiApiKey { get; set; } //съхранява API ключа
        public string GeminiModel { get; set; } = "gemini-3.8-flash"; //кой Gemini модел да се използва
    }
}
