namespace AzureDevOps.Gamification.Api.DTOs
{
    public record ProjectListResponse
    {
        public List<ProjectInfo> Value { get; set; } = [];
    }
}