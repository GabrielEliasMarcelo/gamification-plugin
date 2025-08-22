namespace AzureDevOps.Gamification.Api.DTOs
{
    public record DetailedCommitInfo
    {
        public string CommitId { get; init; } = "";
        public string Author { get; init; } = "";
        public string Message { get; init; } = "";
        public DateTime Date { get; init; }
        public int LinesAdded { get; init; }
        public int LinesDeleted { get; init; }
        public int TotalChanges { get; init; }
        public int FilesChanged { get; init; }
        public List<string> FileTypes { get; init; } = [];
        public string ProjectName { get; init; } = "";
        public string RepositoryName { get; init; } = "";
        public CommitCategory Category { get; init; }
        public CommitSize Size { get; init; }
    }
}