namespace AzureDevOps.Gamification.Api.DTOs
{
    public record CommitAnalysis
    {
        public int TotalCommits { get; init; }
        public CommitSizeMetrics SizeMetrics { get; init; } = new();
        public List<DetailedCommitInfo> TopCommitsBySize { get; init; } = [];
        public List<DetailedCommitInfo> RecentCommits { get; init; } = [];
        public CommitQualityMetrics QualityMetrics { get; init; } = new();
        public Dictionary<string, AuthorCommitStats> AuthorStats { get; init; } = [];
        public Dictionary<string, int> FileTypeDistribution { get; init; } = [];
        public DateTime AnalysisDate { get; init; } = DateTime.UtcNow;
    }
}