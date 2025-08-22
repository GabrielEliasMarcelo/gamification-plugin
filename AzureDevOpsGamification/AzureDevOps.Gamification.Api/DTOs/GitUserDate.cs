namespace AzureDevOps.Gamification.Api.DTOs
{
    public record GitUserDate
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public DateTime Date { get; set; }
    }
}