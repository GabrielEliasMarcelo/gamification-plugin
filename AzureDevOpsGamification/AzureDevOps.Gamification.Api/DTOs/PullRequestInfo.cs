namespace AzureDevOps.Gamification.Api.DTOs
{
    public record PullRequestInfo
    {
        public int PullRequestId { get; set; }
        public string Status { get; set; } = "";
        public string MergeStatus { get; set; } = "";
        public DateTime? CreationDate { get; set; }
        public DateTime? ClosedDate { get; set; }
    }
}