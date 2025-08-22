namespace AzureDevOps.Gamification.Api.DTOs
{
    public record GitDiffResponse
    {
        public List<GitDiffChange> Changes { get; set; } = [];
    }
}