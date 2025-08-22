namespace AzureDevOps.Gamification.Api.DTOs
{
    public record GraphLink
    {
        public string Source { get; set; } = "";
        public string Target { get; set; } = "";
        public int Weight { get; set; }
        public string ProjectName { get; set; } = "";
    }
}