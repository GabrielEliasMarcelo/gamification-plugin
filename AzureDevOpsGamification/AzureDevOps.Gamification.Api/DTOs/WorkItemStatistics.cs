namespace AzureDevOps.Gamification.Api.DTOs
{
    public record WorkItemStatistics
    {
        public int TotalWorkItems { get; init; }
        public int CompletedWorkItems { get; init; }
    }
}