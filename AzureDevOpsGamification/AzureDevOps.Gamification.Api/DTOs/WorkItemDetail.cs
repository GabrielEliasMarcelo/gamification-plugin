namespace AzureDevOps.Gamification.Api.DTOs
{
    public record WorkItemDetail
    {
        public Dictionary<string, object>? Fields { get; set; }
    }
}