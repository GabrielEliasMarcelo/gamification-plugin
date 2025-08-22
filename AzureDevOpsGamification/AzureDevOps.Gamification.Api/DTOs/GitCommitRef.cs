namespace AzureDevOps.Gamification.Api.DTOs
{
    public record GitCommitRef
    {
        public string CommitId { get; set; } = "";
        public GitUserDate Author { get; set; } = new();
        public string Comment { get; set; } = "";
        public List<GitChange>? Changes { get; set; }
        public string? ProjectName { get; set; }
        public string? RepositoryName { get; set; }
        public string? RepositoryId { get; set; }
    }
}