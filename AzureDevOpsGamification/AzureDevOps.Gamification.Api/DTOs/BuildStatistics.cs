namespace AzureDevOps.Gamification.Api.DTOs
{
    public record BuildStatistics
    {
        public int TotalBuilds { get; init; }
        public int SuccessfulBuilds { get; init; }
        public TimeSpan AverageDuration { get; init; }
    }
}