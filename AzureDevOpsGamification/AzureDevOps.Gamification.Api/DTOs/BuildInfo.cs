namespace AzureDevOps.Gamification.Api.DTOs
{
    public record BuildInfo
    {
        public int Id { get; set; }
        public string Result { get; set; } = "";
        public DateTime? StartTime { get; set; }
        public DateTime? FinishTime { get; set; }
    }
}