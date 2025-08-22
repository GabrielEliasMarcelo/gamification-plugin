namespace AzureDevOps.Gamification.Api.DTOs
{
    public record CodeGraphRequest
    {
        public string Organization { get; init; } = "";
        public string Project { get; init; } = "";
        public string? RepositoryId { get; init; }
        public string? Token { get; init; }
    }
}