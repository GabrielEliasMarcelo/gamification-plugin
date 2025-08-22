namespace AzureDevOps.Gamification.Api.DTOs
{
    public record AuthorCommitStats
    {
        public string AuthorName { get; init; } = "";
        public int TotalCommits { get; init; }
        public int TotalLinesAdded { get; init; }
        public int TotalLinesDeleted { get; init; }
        public double AverageLinesPerCommit { get; init; }
        public int LargestCommit { get; init; }
        public DateTime LastCommitDate { get; init; }
        public Dictionary<CommitSize, int> CommitSizeDistribution { get; init; } = [];
        public Dictionary<CommitCategory, int> CommitCategoryDistribution { get; init; } = [];
    }
}