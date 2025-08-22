namespace AzureDevOps.Gamification.Api.DTOs
{
    public record RankingRequest
    {
        public string Organization { get; init; } = "";
        public string Project { get; init; } = "";
        public int? Year { get; init; }
        public int? Month { get; init; }

        public string? Token { get; init; }
    }
}