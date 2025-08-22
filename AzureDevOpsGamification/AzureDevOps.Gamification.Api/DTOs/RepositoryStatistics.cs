namespace AzureDevOps.Gamification.Api.DTOs
{
    public record RepositoryStatistics
    {
        public int TotalRepositories { get; init; }
        public int ActiveRepositories { get; init; }
    }
}