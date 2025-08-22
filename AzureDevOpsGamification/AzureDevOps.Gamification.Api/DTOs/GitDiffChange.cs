namespace AzureDevOps.Gamification.Api.DTOs
{
    public record GitDiffChange
    {
        public string ChangeType { get; set; } = "";
        public int LineNumber { get; set; }
        public string Content { get; set; } = "";
    }
}