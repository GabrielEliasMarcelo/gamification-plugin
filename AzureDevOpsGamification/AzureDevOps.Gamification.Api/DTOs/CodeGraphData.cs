namespace AzureDevOps.Gamification.Api.DTOs
{
    public record CodeGraphData
    {
        public List<GraphNode> Nodes { get; set; } = [];
        public List<GraphLink> Links { get; set; } = [];
    }
}