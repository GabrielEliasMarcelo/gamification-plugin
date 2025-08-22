namespace AzureDevOps.Gamification.Api.DTOs
{
    public record CommitAnalysisRequest
    {
        public string Organization { get; init; } = "";
        public string? Project { get; init; } // Opcional
        public int? Year { get; init; }
        public int? Month { get; init; }
        public string? Author { get; init; }
        public string? RepositoryId { get; init; }
        public string? Token { get; init; } // Token do Azure DevOps
    }
}