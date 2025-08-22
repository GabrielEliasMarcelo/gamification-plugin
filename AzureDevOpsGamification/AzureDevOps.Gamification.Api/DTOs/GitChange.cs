using AzureDevOps.Gamification.Api.Services;

namespace AzureDevOps.Gamification.Api.DTOs
{
    public record GitChange
    {
        public GitItem? Item { get; set; }
    }
}