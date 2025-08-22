namespace AzureDevOps.Gamification.Api.DTOs
{
    public record CommitMetrics
    {
        public int TotalCommits { get; set; }
        public Dictionary<string, int> CommitsByAuthor { get; set; } = [];
        public Dictionary<DateTime, int> CommitsByDate { get; set; } = [];
        public Dictionary<string, int> CommitsByProject { get; set; } = [];
        public Dictionary<string, int> CommitsByRepository { get; set; } = [];
    }
}