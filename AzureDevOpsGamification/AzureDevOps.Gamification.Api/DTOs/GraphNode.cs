namespace AzureDevOps.Gamification.Api.DTOs
{
    public record GraphNode
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public int CommitCount { get; set; }
        public string ProjectName { get; set; } = "";
    }
}