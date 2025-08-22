namespace AzureDevOps.Gamification.Api.DTOs
{
    public record GitChangeDetail
    {
        public GitItem? Item { get; set; }
        public string ChangeType { get; set; } = "";
    }
}