namespace AzureDevOps.Gamification.Api.DTOs
{
    public record CodeCoverageResult
    {
        public List<CoverageData> CoverageData { get; set; } = [];
    }
}