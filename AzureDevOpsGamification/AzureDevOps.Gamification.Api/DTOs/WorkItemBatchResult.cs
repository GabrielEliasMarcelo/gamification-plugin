namespace AzureDevOps.Gamification.Api.DTOs
{
    public record WorkItemBatchResult
    {
        public List<WorkItemDetail> Value { get; set; } = [];
    }
}