namespace AzureDevOps.Gamification.Api.DTOs
{
    public record CoverageStats
    {
        public int Total { get; set; }
        public int Covered { get; set; }
    }
}