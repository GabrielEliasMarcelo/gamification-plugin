namespace AzureDevOps.Gamification.Api.DTOs
{
    public record StatsRequest
    {
        public string Organization { get; init; } = "";
        public string Project { get; init; } = "";
        public int? Days { get; init; }
        public string? Token { get; init; }
    }
}