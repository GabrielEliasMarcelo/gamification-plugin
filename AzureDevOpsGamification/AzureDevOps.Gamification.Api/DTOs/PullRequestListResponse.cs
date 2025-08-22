namespace AzureDevOps.Gamification.Api.DTOs
{
    public record PullRequestListResponse
    {
        public List<PullRequestInfo> Value { get; set; } = [];
    }
}