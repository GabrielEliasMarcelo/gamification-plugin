using AzureDevOps.Gamification.Api.DTOs;

namespace AzureDevOps.Gamification.Api.Services
{
    public interface IAzureDevOpsService
    {
        Task<CommitMetrics> GetCommitMetricsAsync(string organization, string? project, int? year, int? month, string? author, string token);

        Task<IEnumerable<DeveloperRanking>> GetDeveloperRankingAsync(string organization, string? project, int? year, int? month, string token);

        Task<CodeGraphData> GetCodeGraphDataAsync(string organization, string? project, string? repositoryId, string token);

        Task<GeneralStats> GetGeneralStatsAsync(string organization, string? project, int days, string token);

        Task<CommitAnalysis> GetCommitAnalysisAsync(string organization, string? project, int? year, int? month, string? author, string? repositoryId, string token);
    }
}