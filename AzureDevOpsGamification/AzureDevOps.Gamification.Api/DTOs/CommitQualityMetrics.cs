namespace AzureDevOps.Gamification.Api.DTOs
{
    public record CommitQualityMetrics
    {
        public int CommitsWithTests { get; init; }
        public int CommitsWithDocumentation { get; init; }
        public int RefactoringCommits { get; init; }
        public int BugFixCommits { get; init; }
        public int FeatureCommits { get; init; }
        public double AverageFilesPerCommit { get; init; }
        public int SingleFileCommits { get; init; }
        public int MultiFileCommits { get; init; }
    }
}