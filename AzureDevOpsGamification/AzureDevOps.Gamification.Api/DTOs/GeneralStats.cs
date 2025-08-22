namespace AzureDevOps.Gamification.Api.DTOs
{
    public record GeneralStats
    {
        // Build Statistics
        public int TotalBuilds { get; init; }
        public int SuccessfulBuilds { get; init; }
        public double BuildSuccessRate { get; init; }
        public TimeSpan AverageBuildDuration { get; init; }

        // Pull Request Statistics
        public int TotalPullRequests { get; init; }
        public int MergedPullRequests { get; init; }
        public TimeSpan AveragePRTime { get; init; }
        public double PRMergeRate { get; init; }

        // Work Item Statistics
        public int TotalWorkItems { get; init; }
        public int CompletedWorkItems { get; init; }
        public double WorkItemCompletionRate { get; init; }

        // Repository Statistics
        public int TotalRepositories { get; init; }
        public int ActiveRepositories { get; init; }

        // Quality Metrics
        public double CodeCoverage { get; init; }
        public double TestPassRate { get; init; }

        // Activity Metrics
        public double CommitsPerDay { get; init; }
        public int ActiveDevelopers { get; init; }

        // Metadata
        public int DaysAnalyzed { get; init; }
        public DateTime LastUpdated { get; init; }

        // Calculated Properties para facilitar exibição
        public string BuildSuccessRateFormatted => $"{BuildSuccessRate:F1}%";
        public string PRMergeRateFormatted => $"{PRMergeRate:F1}%";
        public string WorkItemCompletionRateFormatted => $"{WorkItemCompletionRate:F1}%";
        public string CodeCoverageFormatted => $"{CodeCoverage:F1}%";
        public string TestPassRateFormatted => $"{TestPassRate:F1}%";
        public string AverageBuildDurationFormatted =>
            AverageBuildDuration.TotalMinutes > 60
                ? $"{AverageBuildDuration.TotalHours:F1}h"
                : $"{AverageBuildDuration.TotalMinutes:F0}min";
        public string AveragePRTimeFormatted =>
            AveragePRTime.TotalDays >= 1
                ? $"{AveragePRTime.TotalDays:F1} dias"
                : $"{AveragePRTime.TotalHours:F1}h";
    }
}