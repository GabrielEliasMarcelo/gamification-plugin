using AzureDevOps.Gamification.Api.Services;

namespace AzureDevOps.Gamification.Api.DTOs
{
    public record GitRepositoryListResponse
    {
        public List<GitRepository> Value { get; set; } = [];
    }
}