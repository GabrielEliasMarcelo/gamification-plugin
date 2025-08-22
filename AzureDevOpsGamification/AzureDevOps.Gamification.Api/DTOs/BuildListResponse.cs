namespace AzureDevOps.Gamification.Api.DTOs
{
    public record BuildListResponse
    {
        public List<BuildInfo> Value { get; set; } = [];
    }
}