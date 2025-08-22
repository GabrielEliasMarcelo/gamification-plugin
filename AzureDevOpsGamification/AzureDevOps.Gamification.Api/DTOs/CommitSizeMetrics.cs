namespace AzureDevOps.Gamification.Api.DTOs
{
    public record CommitSizeMetrics
    {
        public int TotalLinesAdded { get; init; }
        public int TotalLinesDeleted { get; init; }
        public int NetLinesChanged { get; init; }
        public double AverageLinesPerCommit { get; init; }
        public int LargestCommitSize { get; init; }
        public int SmallestCommitSize { get; init; }
        public int CommitsSmall { get; init; } // < 10 linhas
        public int CommitsMedium { get; init; } // 10-100 linhas
        public int CommitsLarge { get; init; } // 100-500 linhas
        public int CommitsHuge { get; init; } // > 500 linhas
    }
}