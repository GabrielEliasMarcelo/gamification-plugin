namespace AzureDevOps.Gamification.Api.DTOs
{
    public record HealthStatus
    {
        public string Status { get; init; } = "";
        public DateTime Timestamp { get; init; }
        public string Version { get; init; } = "";
    }
}