namespace AzureDevOps.Gamification.Api.DTOs
{
    public record GitCommitChangesResponse
    {
        public List<GitChangeDetail> Changes { get; set; } = [];
    }
}