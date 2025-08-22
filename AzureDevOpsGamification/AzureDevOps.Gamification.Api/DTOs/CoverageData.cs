namespace AzureDevOps.Gamification.Api.DTOs
{
    public record CoverageData
    {
        public List<CoverageStats> CoverageStats { get; set; } = [];
    }
}