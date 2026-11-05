using BookLibrary.Services;

namespace BookLibrary.Configuration
{
    public class LocalAiOptions
    {
        public const string SectionName = "LocalAi";

        public bool Enabled { get; set; } = true;

        public string? BaseUrl { get; set; }

        public string? Model { get; set; }

        public string? ApiKey { get; set; }

        public bool SupportsVision { get; set; } = true;

        public int TimeoutSeconds { get; set; } = 60;

        public bool UseJsonResponseFormat { get; set; }

        public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
    }
}
