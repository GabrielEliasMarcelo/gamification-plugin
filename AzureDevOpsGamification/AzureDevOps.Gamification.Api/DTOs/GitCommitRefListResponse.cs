using AzureDevOps.Gamification.Api.Services;

namespace AzureDevOps.Gamification.Api.DTOs
{
    public record GitCommitRefListResponse
    {
        public List<GitCommitRef> Value { get; set; } = [];
    }
}