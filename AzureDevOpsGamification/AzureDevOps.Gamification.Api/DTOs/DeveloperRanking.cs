namespace AzureDevOps.Gamification.Api.DTOs
{
    public record DeveloperRanking
    {
        public string DeveloperName { get; set; } = "";
        public int CommitCount { get; set; }
        public double Score { get; set; }
        public DateTime LastCommitDate { get; set; }
        public int ProjectsContributed { get; set; }
        public int RepositoriesContributed { get; set; }
    }
}