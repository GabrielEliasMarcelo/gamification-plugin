namespace AzureDevOps.Gamification.Api.DTOs
{
    public record ProjectInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
    }
}