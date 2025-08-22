namespace AzureDevOps.Gamification.Api.DTOs
{
    public record GitRepository
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public ProjectInfo? Project { get; set; }
    }
}