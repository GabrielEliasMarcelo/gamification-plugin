namespace AzureDevOps.Gamification.Api.DTOs
{
    public record PullRequestStatistics
    {
        public int TotalPRs { get; init; }
        public int MergedPRs { get; init; }
        public TimeSpan AveragePRTime { get; init; }
    }
}