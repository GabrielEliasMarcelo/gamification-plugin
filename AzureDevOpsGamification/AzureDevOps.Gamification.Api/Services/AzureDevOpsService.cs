using AzureDevOps.Gamification.Api.DTOs;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;
using System.Text.Json;

namespace AzureDevOps.Gamification.Api.Services;

public class AzureDevOpsService : IAzureDevOpsService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AzureDevOpsService> _logger;

    public AzureDevOpsService(HttpClient httpClient, IMemoryCache cache, ILogger<AzureDevOpsService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<CommitMetrics> GetCommitMetricsAsync(string organization, string? project, int? year, int? month, string? author, string token)
    {
        // Cache key baseado em parâmetros - incluindo indicador se é toda organização
        var cacheKey = $"commits_{organization}_{project ?? "ALL"}_{year}_{month}_{author}";

        if (_cache.TryGetValue(cacheKey, out CommitMetrics? cachedMetrics))
            return cachedMetrics!;

        var commits = await GetCommitsFromApiAsync(organization, project, year, month, author, token);

        // Algoritmo de cálculo de métricas - agregação simples baseada em contagem
        var metrics = new CommitMetrics
        {
            TotalCommits = commits.Count,

            CommitsByAuthor = commits.GroupBy(c => c.Author.Name)
                                .OrderByDescending(g => g.Count())
                                .ToDictionary(g => g.Key, g => g.Count()),

            CommitsByDate = commits.GroupBy(c => c.Author.Date.Date)
                               .OrderByDescending(g => g.Count())
                               .ToDictionary(g => g.Key, g => g.Count()),

            CommitsByProject = commits.GroupBy(c => c.ProjectName ?? "Unknown")
                                 .OrderByDescending(g => g.Count())
                                 .ToDictionary(g => g.Key, g => g.Count()),

            CommitsByRepository = commits.GroupBy(c => c.RepositoryName ?? "Unknown")
                                    .OrderByDescending(g => g.Count())
                                    .ToDictionary(g => g.Key, g => g.Count())
        };

        // Cache com TTL de 5 minutos - balance entre performance e dados atualizados
        //_cache.Set(cacheKey, metrics, TimeSpan.FromMinutes(5));

        return metrics;
    }

    public async Task<IEnumerable<DeveloperRanking>> GetDeveloperRankingAsync(string organization, string? project, int? year, int? month, string token)
    {
        var commits = await GetCommitsFromApiAsync(organization, project, year, month, null, token);

        // Algoritmo de ranking: peso para commits recentes + frequência + diversidade de projetos
        var ranking = commits
            .GroupBy(c => c.Author.Name)
            .Select(g => new DeveloperRanking
            {
                DeveloperName = g.Key,
                CommitCount = g.Count(),
                // Score baseado em commits + recência + diversidade de projetos
                Score = g.Sum(commit =>
                {
                    var daysSinceCommit = (DateTime.Now - commit.Author.Date).Days;
                    var recencyBonus = Math.Max(0, 30 - daysSinceCommit) / 30.0; // Bonus decai em 30 dias
                    return 1.0 + recencyBonus;
                }),
                LastCommitDate = g.Max(c => c.Author.Date),
                ProjectsContributed = g.Select(c => c.ProjectName).Distinct().Count(),
                RepositoriesContributed = g.Select(c => c.RepositoryName).Distinct().Count()
            })
            .OrderByDescending(r => r.Score)
            .ToList();

        return ranking;
    }

    public async Task<CodeGraphData> GetCodeGraphDataAsync(string organization, string? project, string? repositoryId, string token)
    {
        var repositories = await GetRepositoriesAsync(organization, project, token);
        var targetRepo = string.IsNullOrEmpty(repositoryId) ? repositories.FirstOrDefault() :
                        repositories.FirstOrDefault(r => r.Id == repositoryId);

        if (targetRepo == null && repositories.Any())
        {
            targetRepo = repositories.First();
        }

        if (targetRepo == null)
            return new CodeGraphData { Nodes = [], Links = [] };

        // Busca commits recentes para construir grafo de colaboração
        var commits = await GetCommitsFromApiAsync(organization, project, DateTime.Now.Year, DateTime.Now.Month, null, token);

        // Algoritmo de construção do grafo: arquivos como nós, commits como arestas
        var fileNodes = commits
            .SelectMany(c => c.Changes?.Select(ch => ch.Item?.Path) ?? [])
            .Where(path => !string.IsNullOrEmpty(path))
            .Distinct()
            .Select((path, index) => new GraphNode
            {
                Id = path!,
                Name = Path.GetFileName(path!),
                Type = "file",
                CommitCount = commits.Count(c => c.Changes?.Any(ch => ch.Item?.Path == path) == true),
                ProjectName = commits.Where(c => c.Changes?.Any(ch => ch.Item?.Path == path) == true)
                                   .Select(c => c.ProjectName)
                                   .FirstOrDefault() ?? "Unknown"
            })
            .ToList();

        var authorNodes = commits
            .Select(c => c.Author.Name)
            .Distinct()
            .Select(author => new GraphNode
            {
                Id = author,
                Name = author,
                Type = "author",
                CommitCount = commits.Count(c => c.Author.Name == author),
                ProjectName = commits.Where(c => c.Author.Name == author)
                                   .Select(c => c.ProjectName)
                                   .GroupBy(p => p)
                                   .OrderByDescending(g => g.Count())
                                   .FirstOrDefault()?.Key ?? "Unknown"
            })
            .ToList();

        // Links baseados em co-modificação de arquivos
        var links = commits
            .SelectMany(commit =>
                (commit.Changes ?? [])
                    .Where(c => !string.IsNullOrEmpty(c.Item?.Path))
                    .Select(change => new GraphLink
                    {
                        Source = commit.Author.Name,
                        Target = change.Item!.Path!,
                        Weight = 1,
                        ProjectName = commit.ProjectName ?? "Unknown"
                    }))
            .GroupBy(l => new { l.Source, l.Target })
            .Select(g => new GraphLink
            {
                Source = g.Key.Source,
                Target = g.Key.Target,
                Weight = g.Sum(l => l.Weight),
                ProjectName = g.First().ProjectName
            })
            .ToList();

        return new CodeGraphData
        {
            Nodes = [.. fileNodes, .. authorNodes],
            Links = links
        };
    }

    public async Task<GeneralStats> GetGeneralStatsAsync(string organization, string? project, int days, string token)
    {
        // Cache key baseado em parâmetros - incluindo indicador se é toda organização
        var cacheKey = $"general_stats_{organization}_{project ?? "ALL"}_{days}";

        if (_cache.TryGetValue(cacheKey, out GeneralStats? cachedStats))
            return cachedStats!;

        try
        {
            // Configuração de autenticação
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization",
                $"Basic {Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{token}"))}");

            var fromDate = DateTime.Now.AddDays(-days);
            var toDate = DateTime.Now;

            // Buscar dados em paralelo com tratamento de erro individual
            var buildStatsTask = SafeExecuteAsync(() => GetBuildStatsAsync(organization, project, fromDate, toDate), new BuildStatistics());
            var prStatsTask = SafeExecuteAsync(() => GetPullRequestStatsAsync(organization, project, fromDate, toDate), new PullRequestStatistics());
            var wiStatsTask = SafeExecuteAsync(() => GetWorkItemStatsAsync(organization, project, fromDate, toDate), new WorkItemStatistics());
            var repoStatsTask = SafeExecuteAsync(() => GetRepositoryStatsAsync(organization, project), new RepositoryStatistics());

            // Buscar métricas adicionais em paralelo
            var codeCoverageTask = SafeExecuteAsync(() => GetCodeCoverageAsync(organization, project, fromDate, toDate), 0.0);
            //var testPassRateTask = SafeExecuteAsync(() => GetTestPassRateAsync(organization, project, fromDate, toDate), 0.0);
            var commitsPerDayTask = SafeExecuteAsync(() => GetAvgCommitsPerDayAsync(organization, project, days, token), 0.0);
            var activeDevelopersTask = SafeExecuteAsync(() => GetActiveDevelopersCountAsync(organization, project, fromDate, toDate, token), 0);

            // Aguardar todas as tasks
            await Task.WhenAll(
                buildStatsTask, prStatsTask, wiStatsTask, repoStatsTask,
                codeCoverageTask, commitsPerDayTask, activeDevelopersTask
            );

            // Obter resultados
            var buildStats = await buildStatsTask;
            var prStats = await prStatsTask;
            var wiStats = await wiStatsTask;
            var repoStats = await repoStatsTask;
            var codeCoverage = await codeCoverageTask;
            //var testPassRate = await testPassRateTask;
            var commitsPerDay = await commitsPerDayTask;
            var activeDevelopers = await activeDevelopersTask;

            // Calcular estatísticas consolidadas
            var generalStats = new GeneralStats
            {
                // Build Statistics
                TotalBuilds = buildStats.TotalBuilds,
                SuccessfulBuilds = buildStats.SuccessfulBuilds,
                BuildSuccessRate = buildStats.TotalBuilds > 0
                    ? Math.Round((double)buildStats.SuccessfulBuilds / buildStats.TotalBuilds * 100, 2)
                    : 0,
                AverageBuildDuration = buildStats.AverageDuration,

                // Pull Request Statistics
                TotalPullRequests = prStats.TotalPRs,
                MergedPullRequests = prStats.MergedPRs,
                AveragePRTime = prStats.AveragePRTime,
                PRMergeRate = prStats.TotalPRs > 0
                    ? Math.Round((double)prStats.MergedPRs / prStats.TotalPRs * 100, 2)
                    : 0,

                // Work Item Statistics
                TotalWorkItems = wiStats.TotalWorkItems,
                CompletedWorkItems = wiStats.CompletedWorkItems,
                WorkItemCompletionRate = wiStats.TotalWorkItems > 0
                    ? Math.Round((double)wiStats.CompletedWorkItems / wiStats.TotalWorkItems * 100, 2)
                    : 0,

                // Repository Statistics
                TotalRepositories = repoStats.TotalRepositories,
                ActiveRepositories = repoStats.ActiveRepositories,

                // Quality Metrics
                CodeCoverage = codeCoverage,
                //TestPassRate = testPassRate,

                // Activity Metrics
                CommitsPerDay = commitsPerDay,
                ActiveDevelopers = activeDevelopers,

                // Metadata
                DaysAnalyzed = days,
                LastUpdated = DateTime.UtcNow
            };

            // Cache com TTL de 15 minutos para stats gerais
            _cache.Set(cacheKey, generalStats, TimeSpan.FromMinutes(15));

            return generalStats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter estatísticas gerais para {Organization}/{Project}",
                organization, project ?? "ALL");

            // Retorna stats vazias em caso de erro
            return new GeneralStats
            {
                DaysAnalyzed = days,
                LastUpdated = DateTime.UtcNow
            };
        }
    }

    public async Task<CommitAnalysis> GetCommitAnalysisAsync(string organization, string? project, int? year, int? month, string? author, string? repositoryId, string token)
    {
        // Cache key para análise detalhada
        var cacheKey = $"commit_analysis_{organization}_{project ?? "ALL"}_{year}_{month}_{author}_{repositoryId}";

        if (_cache.TryGetValue(cacheKey, out CommitAnalysis? cachedAnalysis))
            return cachedAnalysis!;

        try
        {
            // Buscar commits básicos
            var commits = await GetCommitsFromApiAsync(organization, project, year, month, author, token);

            // Buscar detalhes adicionais para cada commit (changes/diffs)
            var detailedCommits = await GetDetailedCommitInfoAsync(organization, commits, repositoryId, token);

            // Analisar métricas de tamanho
            var sizeMetrics = AnalyzeCommitSizes(detailedCommits);

            // Analisar métricas de qualidade
            var qualityMetrics = AnalyzeCommitQuality(detailedCommits);

            // Estatísticas por autor
            var authorStats = AnalyzeAuthorStats(detailedCommits);

            // Distribuição por tipo de arquivo
            var fileTypeDistribution = AnalyzeFileTypeDistribution(detailedCommits);

            var analysis = new CommitAnalysis
            {
                TotalCommits = detailedCommits.Count,
                SizeMetrics = sizeMetrics,
                TopCommitsBySize = detailedCommits
                    .OrderByDescending(c => c.TotalChanges)
                    .Take(10)
                    .ToList(),
                RecentCommits = detailedCommits
                    .OrderByDescending(c => c.Date)
                    .Take(20)
                    .ToList(),
                QualityMetrics = qualityMetrics,
                AuthorStats = authorStats,
                FileTypeDistribution = fileTypeDistribution,
                AnalysisDate = DateTime.UtcNow
            };

            // Cache com TTL de 10 minutos (análise mais pesada)
            //_cache.Set(cacheKey, analysis, TimeSpan.FromMinutes(10));

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao analisar commits para {Organization}/{Project}",
                organization, project ?? "ALL");

            return new CommitAnalysis
            {
                TotalCommits = 0,
                AnalysisDate = DateTime.UtcNow
            };
        }
    }

    private async Task<List<DetailedCommitInfo>> GetDetailedCommitInfoAsync(string organization, List<GitCommitRef> commits, string? repositoryId, string token)
    {
        var detailedCommits = new List<DetailedCommitInfo>();
        var processedCommits = 0;
        const int maxCommitsToProcess = 100; // Limitar para performance

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization",
            $"Basic {Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{token}"))}");

        foreach (var commit in commits.Take(maxCommitsToProcess))
        {
            try
            {
                // Se repositoryId específico foi fornecido, filtrar apenas commits desse repo
                if (!string.IsNullOrEmpty(repositoryId) && commit.RepositoryId != repositoryId)
                    continue;

                var commitDetails = await GetCommitChangesAsync(organization, commit, token);
                if (commitDetails != null)
                {
                    detailedCommits.Add(commitDetails);
                }

                processedCommits++;

                // Rate limiting mais agressivo para chamadas detalhadas
                await Task.Delay(150);

                // Log de progresso para commits grandes
                if (processedCommits % 20 == 0)
                {
                    _logger.LogInformation("Processados {Count}/{Total} commits para análise detalhada",
                        processedCommits, Math.Min(commits.Count, maxCommitsToProcess));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao processar commit {CommitId}", commit.CommitId);
            }
        }

        return detailedCommits;
    }

    private async Task<DetailedCommitInfo?> GetCommitChangesAsync(string organization, GitCommitRef commit, string token)
    {
        try
        {
            // Buscar detalhes das mudanças (diff) do commit
            var url = $"https://dev.azure.com/{organization}/{commit.ProjectName}/_apis/git/repositories/{commit.RepositoryId}/commits/{commit.CommitId}/changes?api-version=7.1-preview.1";

            var response = await _httpClient.GetStringAsync(url);
            var changesResult = JsonSerializer.Deserialize<GitCommitChangesResponse>(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (changesResult?.Changes == null)
                return null;

            // Calcular métricas do commit
            var linesAdded = 0;
            var linesDeleted = 0;
            var fileTypes = new List<string>();

            foreach (var change in changesResult.Changes)
            {
                if (change.Item?.Path != null)
                {
                    var extension = Path.GetExtension(change.Item.Path).ToLowerInvariant();
                    if (!string.IsNullOrEmpty(extension))
                    {
                        fileTypes.Add(extension);
                    }
                }

                // Tentar obter estatísticas de linha (se disponível)
                if (change.ChangeType == "edit" || change.ChangeType == "add")
                {
                    try
                    {
                        var diffStats = await GetDiffStatsAsync(organization, commit.ProjectName!, commit.RepositoryId!,
                            commit.CommitId, change.Item?.Path, token);
                        linesAdded += diffStats.LinesAdded;
                        linesDeleted += diffStats.LinesDeleted;
                    }
                    catch
                    {
                        // Se não conseguir obter diff exato, estimar baseado no tipo de mudança
                        if (change.ChangeType == "add")
                        {
                            linesAdded += EstimateFileSize(change.Item?.Path);
                        }
                        else if (change.ChangeType == "edit")
                        {
                            linesAdded += 5; // Estimativa conservadora
                            linesDeleted += 3;
                        }
                    }
                }
                else if (change.ChangeType == "delete")
                {
                    linesDeleted += EstimateFileSize(change.Item?.Path);
                }
            }

            return new DetailedCommitInfo
            {
                CommitId = commit.CommitId,
                Author = commit.Author.Name,
                Message = commit.Comment,
                Date = commit.Author.Date,
                LinesAdded = linesAdded,
                LinesDeleted = linesDeleted,
                TotalChanges = linesAdded + linesDeleted,
                FilesChanged = changesResult.Changes.Count,
                FileTypes = fileTypes.Distinct().ToList(),
                ProjectName = commit.ProjectName ?? "Unknown",
                RepositoryName = commit.RepositoryName ?? "Unknown",
                Category = CategorizeCommit(commit.Comment),
                Size = CategorizeCommitSize(linesAdded + linesDeleted)
            };
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("404"))
        {
            // Commit pode ter sido removido ou não ter permissão
            _logger.LogWarning("Commit {CommitId} não encontrado ou sem permissão", commit.CommitId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao obter detalhes do commit {CommitId}", commit.CommitId);
            return null;
        }
    }

    private async Task<(int LinesAdded, int LinesDeleted)> GetDiffStatsAsync(string organization, string project, string repositoryId, string commitId, string? filePath, string token)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
                return (0, 0);

            // Obter diff do arquivo específico
            var url = $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}/diffs/commits/{commitId}?path={Uri.EscapeDataString(filePath)}&api-version=7.1-preview.1";

            var response = await _httpClient.GetStringAsync(url);
            var diffResult = JsonSerializer.Deserialize<GitDiffResponse>(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            // Análise simplificada do diff
            var linesAdded = 0;
            var linesDeleted = 0;

            if (diffResult?.Changes != null)
            {
                foreach (var change in diffResult.Changes)
                {
                    if (change.ChangeType == "add")
                        linesAdded++;
                    else if (change.ChangeType == "delete")
                        linesDeleted++;
                }
            }

            return (linesAdded, linesDeleted);
        }
        catch
        {
            return (0, 0);
        }
    }

    private static int EstimateFileSize(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return 10;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".cs" or ".java" or ".py" or ".js" or ".ts" => 50,
            ".html" or ".xml" or ".json" => 30,
            ".css" or ".scss" => 25,
            ".md" or ".txt" => 20,
            ".config" or ".yml" or ".yaml" => 15,
            _ => 10
        };
    }

    private static CommitCategory CategorizeCommit(string message)
    {
        var lowerMessage = message.ToLowerInvariant();

        if (lowerMessage.Contains("fix") || lowerMessage.Contains("bug") || lowerMessage.Contains("error"))
            return CommitCategory.BugFix;

        if (lowerMessage.Contains("test") || lowerMessage.Contains("spec"))
            return CommitCategory.Test;

        if (lowerMessage.Contains("refactor") || lowerMessage.Contains("cleanup") || lowerMessage.Contains("improve"))
            return CommitCategory.Refactoring;

        if (lowerMessage.Contains("doc") || lowerMessage.Contains("readme") || lowerMessage.Contains("comment"))
            return CommitCategory.Documentation;

        if (lowerMessage.Contains("config") || lowerMessage.Contains("setting") || lowerMessage.Contains(".config"))
            return CommitCategory.Configuration;

        if (lowerMessage.Contains("feat") || lowerMessage.Contains("add") || lowerMessage.Contains("implement"))
            return CommitCategory.Feature;

        return CommitCategory.Other;
    }

    private static CommitSize CategorizeCommitSize(int totalChanges)
    {
        return totalChanges switch
        {
            < 10 => CommitSize.Small,
            < 100 => CommitSize.Medium,
            < 500 => CommitSize.Large,
            _ => CommitSize.Huge
        };
    }

    private static CommitSizeMetrics AnalyzeCommitSizes(List<DetailedCommitInfo> commits)
    {
        if (!commits.Any())
            return new CommitSizeMetrics();

        var totalAdded = commits.Sum(c => c.LinesAdded);
        var totalDeleted = commits.Sum(c => c.LinesDeleted);
        var sizes = commits.Select(c => c.TotalChanges).ToList();

        return new CommitSizeMetrics
        {
            TotalLinesAdded = totalAdded,
            TotalLinesDeleted = totalDeleted,
            NetLinesChanged = totalAdded - totalDeleted,
            AverageLinesPerCommit = Math.Round((double)sizes.Sum() / commits.Count, 2),
            LargestCommitSize = sizes.Max(),
            SmallestCommitSize = sizes.Min(),
            CommitsSmall = commits.Count(c => c.Size == CommitSize.Small),
            CommitsMedium = commits.Count(c => c.Size == CommitSize.Medium),
            CommitsLarge = commits.Count(c => c.Size == CommitSize.Large),
            CommitsHuge = commits.Count(c => c.Size == CommitSize.Huge)
        };
    }

    private static CommitQualityMetrics AnalyzeCommitQuality(List<DetailedCommitInfo> commits)
    {
        if (!commits.Any())
            return new CommitQualityMetrics();

        var testCommits = commits.Count(c => c.Category == CommitCategory.Test ||
            c.FileTypes.Any(ft => ft.Contains("test") || ft.Contains("spec")));

        var docCommits = commits.Count(c => c.Category == CommitCategory.Documentation ||
            c.FileTypes.Any(ft => ft == ".md" || ft == ".txt"));

        return new CommitQualityMetrics
        {
            CommitsWithTests = testCommits,
            CommitsWithDocumentation = docCommits,
            RefactoringCommits = commits.Count(c => c.Category == CommitCategory.Refactoring),
            BugFixCommits = commits.Count(c => c.Category == CommitCategory.BugFix),
            FeatureCommits = commits.Count(c => c.Category == CommitCategory.Feature),
            AverageFilesPerCommit = Math.Round((double)commits.Sum(c => c.FilesChanged) / commits.Count, 2),
            SingleFileCommits = commits.Count(c => c.FilesChanged == 1),
            MultiFileCommits = commits.Count(c => c.FilesChanged > 1)
        };
    }

    private static Dictionary<string, AuthorCommitStats> AnalyzeAuthorStats(List<DetailedCommitInfo> commits)
    {
        return commits
            .GroupBy(c => c.Author)
            .ToDictionary(g => g.Key, g =>
            {
                var authorCommits = g.ToList();
                var sizes = authorCommits.Select(c => c.TotalChanges).ToList();

                return new AuthorCommitStats
                {
                    AuthorName = g.Key,
                    TotalCommits = authorCommits.Count,
                    TotalLinesAdded = authorCommits.Sum(c => c.LinesAdded),
                    TotalLinesDeleted = authorCommits.Sum(c => c.LinesDeleted),
                    AverageLinesPerCommit = sizes.Any() ? Math.Round((double)sizes.Sum() / sizes.Count, 2) : 0,
                    LargestCommit = sizes.Any() ? sizes.Max() : 0,
                    LastCommitDate = authorCommits.Max(c => c.Date),
                    CommitSizeDistribution = authorCommits
                        .GroupBy(c => c.Size)
                        .ToDictionary(sg => sg.Key, sg => sg.Count()),
                    CommitCategoryDistribution = authorCommits
                        .GroupBy(c => c.Category)
                        .ToDictionary(cg => cg.Key, cg => cg.Count())
                };
            });
    }

    private static Dictionary<string, int> AnalyzeFileTypeDistribution(List<DetailedCommitInfo> commits)
    {
        return commits
            .SelectMany(c => c.FileTypes)
            .GroupBy(ft => ft)
            .ToDictionary(g => g.Key, g => g.Count())
            .OrderByDescending(kvp => kvp.Value)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private async Task<BuildStatistics> GetBuildStatsAsync(string organization, string? project, DateTime fromDate, DateTime toDate)
    {
        try
        {
            if (!string.IsNullOrEmpty(project))
            {
                return await GetBuildStatsForProjectAsync(organization, project, fromDate, toDate);
            }
            else
            {
                // Para toda organização, buscar builds de todos os projetos
                var projects = await GetAllProjectsAsync(organization);
                var allBuilds = new List<BuildInfo>();

                foreach (var proj in projects.Take(10)) // Limitar para performance
                {
                    try
                    {
                        var projectBuilds = await GetBuildsForProjectAsync(organization, proj.Name, fromDate, toDate);
                        allBuilds.AddRange(projectBuilds);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao buscar builds do projeto {ProjectName}", proj.Name);
                    }

                    await Task.Delay(100); // Rate limiting
                }

                return CalculateBuildStatistics(allBuilds);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar estatísticas de builds");
            return new BuildStatistics();
        }
    }

    private async Task<BuildStatistics> GetBuildStatsForProjectAsync(string organization, string project, DateTime fromDate, DateTime toDate)
    {
        var builds = await GetBuildsForProjectAsync(organization, project, fromDate, toDate);
        return CalculateBuildStatistics(builds);
    }

    private async Task<List<BuildInfo>> GetBuildsForProjectAsync(string organization, string project, DateTime fromDate, DateTime toDate)
    {
        var allBuilds = new List<BuildInfo>();
        var skip = 0;
        const int pageSize = 100;

        var url = $"https://dev.azure.com/{organization}/{project}/_apis/build/builds" +
                 $"?minTime={fromDate:yyyy-MM-ddTHH:mm:ssZ}" +
                 $"&maxTime={toDate:yyyy-MM-ddTHH:mm:ssZ}" +
                 $"&api-version=7.1-preview.7";

        while (true)
        {
            var pagedUrl = $"{url}&$top={pageSize}&$skip={skip}";
            var response = await _httpClient.GetStringAsync(pagedUrl);
            var result = JsonSerializer.Deserialize<BuildListResponse>(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (result?.Value == null || !result.Value.Any())
                break;

            allBuilds.AddRange(result.Value);

            if (result.Value.Count < pageSize)
                break;

            skip += pageSize;
            await Task.Delay(100);
        }

        return allBuilds;
    }

    private static BuildStatistics CalculateBuildStatistics(List<BuildInfo> builds)
    {
        var successfulBuilds = builds.Count(b => b.Result == "succeeded");
        var avgDuration = builds.Where(b => b.FinishTime.HasValue && b.StartTime.HasValue)
                                .Select(b => b.FinishTime!.Value - b.StartTime!.Value)
                                .Where(duration => duration.TotalMinutes > 0)
                                .DefaultIfEmpty(TimeSpan.Zero)
                                .Average(ts => ts.TotalMinutes);

        return new BuildStatistics
        {
            TotalBuilds = builds.Count,
            SuccessfulBuilds = successfulBuilds,
            AverageDuration = TimeSpan.FromMinutes(avgDuration)
        };
    }

    private async Task<T> SafeExecuteAsync<T>(Func<Task<T>> taskFactory, T defaultValue)
    {
        try
        {
            return await taskFactory();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao executar operação, usando valor padrão: {DefaultValue}", defaultValue);
            return defaultValue;
        }
    }

    private async Task<List<GitCommitRef>> GetCommitsFromApiAsync(string organization, string? project, int? year, int? month, string? author, string token)
    {
        // Configuração de autenticação via PAT - padrão Azure DevOps
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{token}"))}");

        var fromDate = year.HasValue && month.HasValue ?
            new DateTime(year.Value, month.Value, 1) :
            DateTime.Now.AddMonths(-12);

        var toDate = year.HasValue && month.HasValue ?
            new DateTime(year.Value, month.Value, DateTime.DaysInMonth(year.Value, month.Value)) :
            DateTime.Now;

        var allCommits = new List<GitCommitRef>();

        if (!string.IsNullOrEmpty(project))
        {
            // Buscar commits de um projeto específico
            var projectCommits = await GetCommitsFromProjectAsync(organization, project, fromDate, toDate, author, token);
            allCommits.AddRange(projectCommits);
        }
        else
        {
            // Buscar commits de toda a organização
            var projects = await GetAllProjectsAsync(organization);

            foreach (var proj in projects.Take(20)) // Limitar a 20 projetos para performance
            {
                try
                {
                    var projectCommits = await GetCommitsFromProjectAsync(organization, proj.Name, fromDate, toDate, author, token);

                    // Adicionar informações do projeto aos commits
                    foreach (var commit in projectCommits)
                    {
                        commit.ProjectName = proj.Name;
                    }

                    allCommits.AddRange(projectCommits);

                    // Rate limiting entre projetos
                    await Task.Delay(200);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao buscar commits do projeto {ProjectName}", proj.Name);
                }
            }
        }

        return allCommits;
    }

    private async Task<List<GitCommitRef>> GetCommitsFromProjectAsync(string organization, string project, DateTime fromDate, DateTime toDate, string? author, string token)
    {
        var allCommits = new List<GitCommitRef>();

        // Primeiro, buscar repositórios do projeto
        var repositories = await GetRepositoriesAsync(organization, project, token);

        foreach (var repo in repositories)
        {
            try
            {
                var skip = 0;
                const int pageSize = 100;

                while (true)
                {
                    var url = $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repo.Id}/commits" +
                             $"?searchCriteria.fromDate={fromDate:yyyy-MM-ddTHH:mm:ssZ}" +
                             $"&searchCriteria.toDate={toDate:yyyy-MM-ddTHH:mm:ssZ}" +
                             $"&$top={pageSize}&$skip={skip}&api-version=7.1-preview.1";

                    if (!string.IsNullOrEmpty(author))
                        url += $"&searchCriteria.author={author}";

                    var response = await _httpClient.GetStringAsync(url);
                    var result = JsonSerializer.Deserialize<GitCommitRefListResponse>(response, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });

                    if (result?.Value == null || !result.Value.Any())
                        break;

                    // Adicionar informações do repositório aos commits
                    foreach (var commit in result.Value)
                    {
                        commit.RepositoryName = repo.Name;
                        commit.RepositoryId = repo.Id;
                        commit.ProjectName = project;
                    }

                    allCommits.AddRange(result.Value);

                    // Rate limiting - evita throttling da API
                    await Task.Delay(100);

                    if (result.Value.Count < pageSize)
                        break;

                    skip += pageSize;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Erro ao buscar commits do repositório {RepoName} no projeto {ProjectName}", repo.Name, project);
            }
        }

        return allCommits;
    }

    private async Task<List<ProjectInfo>> GetAllProjectsAsync(string organization)
    {
        try
        {
            var url = $"https://dev.azure.com/{organization}/_apis/projects?api-version=7.1-preview.4";

            var response = await _httpClient.GetStringAsync(url);
            var result = JsonSerializer.Deserialize<ProjectListResponse>(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            return result?.Value ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro ao buscar projetos da organização {Organization}", organization);
            return [];
        }
    }

    private async Task<List<GitRepository>> GetRepositoriesAsync(string organization, string? project, string token)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{token}"))}");

        try
        {
            string url;
            if (!string.IsNullOrEmpty(project))
            {
                url = $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories?api-version=7.1";
            }
            else
            {
                url = $"https://dev.azure.com/{organization}/_apis/git/repositories?api-version=7.1";
            }

            var response = await _httpClient.GetStringAsync(url);
            var result = JsonSerializer.Deserialize<GitRepositoryListResponse>(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            return result?.Value ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro ao buscar repositórios da organização {Organization}, projeto {Project}", organization, project);
            return null;
        }
    }

    private async Task<PullRequestStatistics> GetPullRequestStatsAsync(string organization, string? project, DateTime fromDate, DateTime toDate)
    {
        try
        {
            var allPRs = new List<PullRequestInfo>();

            if (!string.IsNullOrEmpty(project))
            {
                // Buscar PRs de um projeto específico
                var repos = await GetRepositoriesAsync(organization, project, "");
                await ProcessRepositoriesForPRs(organization, repos, fromDate, toDate, allPRs);
            }
            else
            {
                // Buscar PRs de toda a organização
                var projects = await GetAllProjectsAsync(organization);

                foreach (var proj in projects.Take(10)) // Limitar para performance
                {
                    try
                    {
                        var repos = await GetRepositoriesAsync(organization, proj.Name, "");
                        await ProcessRepositoriesForPRs(organization, repos, fromDate, toDate, allPRs);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao buscar PRs do projeto {ProjectName}", proj.Name);
                    }

                    await Task.Delay(200); // Rate limiting entre projetos
                }
            }

            return CalculatePullRequestStatistics(allPRs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar estatísticas de Pull Requests");
            return new PullRequestStatistics();
        }
    }

    private async Task ProcessRepositoriesForPRs(string organization, List<GitRepository> repos, DateTime fromDate, DateTime toDate, List<PullRequestInfo> allPRs)
    {
        foreach (var repo in repos.Take(5)) // Limitar repos por projeto
        {
            try
            {
                var projectName = repo.Project?.Name ?? "Unknown";
                var prUrl = $"https://dev.azure.com/{organization}/{projectName}/_apis/git/repositories/{repo.Id}/pullrequests" +
                           $"?searchCriteria.status=all" +
                           $"&searchCriteria.minTime={fromDate:yyyy-MM-ddTHH:mm:ssZ}" +
                           $"&searchCriteria.maxTime={toDate:yyyy-MM-ddTHH:mm:ssZ}" +
                           $"&api-version=7.1-preview.1";

                var prResponse = await _httpClient.GetStringAsync(prUrl);
                var prResult = JsonSerializer.Deserialize<PullRequestListResponse>(prResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                if (prResult?.Value != null)
                {
                    allPRs.AddRange(prResult.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao buscar PRs do repositório {RepoId}", repo.Id);
            }

            await Task.Delay(100);
        }
    }

    private static PullRequestStatistics CalculatePullRequestStatistics(List<PullRequestInfo> allPRs)
    {
        var mergedPRs = allPRs.Count(pr => pr.Status == "completed" && pr.MergeStatus == "succeeded");

        var prTimes = allPRs.Where(pr => pr.CreationDate.HasValue && pr.ClosedDate.HasValue)
                           .Select(pr => pr.ClosedDate!.Value - pr.CreationDate!.Value)
                           .Where(duration => duration.TotalHours > 0)
                           .ToList();

        var avgPRTime = prTimes.Any()
            ? TimeSpan.FromHours(prTimes.Average(ts => ts.TotalHours))
            : TimeSpan.Zero;

        return new PullRequestStatistics
        {
            TotalPRs = allPRs.Count,
            MergedPRs = mergedPRs,
            AveragePRTime = avgPRTime
        };
    }

    private async Task<WorkItemStatistics> GetWorkItemStatsAsync(string organization, string? project, DateTime fromDate, DateTime toDate)
    {
        try
        {
            var totalWorkItems = 0;
            var completedWorkItems = 0;

            if (!string.IsNullOrEmpty(project))
            {
                // Buscar work items de um projeto específico
                var (total, completed) = await GetWorkItemsForProject(organization, project, fromDate, toDate);
                totalWorkItems = total;
                completedWorkItems = completed;
            }
            else
            {
                // Buscar work items de toda a organização
                var projects = await GetAllProjectsAsync(organization);

                foreach (var proj in projects.Take(10))
                {
                    try
                    {
                        var (total, completed) = await GetWorkItemsForProject(organization, proj.Name, fromDate, toDate);
                        totalWorkItems += total;
                        completedWorkItems += completed;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao buscar work items do projeto {ProjectName}", proj.Name);
                    }

                    await Task.Delay(100);
                }
            }

            return new WorkItemStatistics
            {
                TotalWorkItems = totalWorkItems,
                CompletedWorkItems = completedWorkItems
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar estatísticas de Work Items");
            return new WorkItemStatistics();
        }
    }

    private async Task<(int total, int completed)> GetWorkItemsForProject(string organization, string project, DateTime fromDate, DateTime toDate)
    {
        var wiql = @"
        SELECT [System.Id], [System.State]
        FROM WorkItems
        WHERE [System.TeamProject] = @project
        AND [System.CreatedDate] >= @startDate
        AND [System.CreatedDate] <= @endDate
        ORDER BY [System.ChangedDate] DESC";

        var wiqlRequest = new
        {
            query = wiql.Replace("@project", $"'{project}'")
                       .Replace("@startDate", $"'{fromDate:yyyy-MM-dd}'")
                       .Replace("@endDate", $"'{toDate:yyyy-MM-dd}'")
        };

        var wiqlUrl = $"https://dev.azure.com/{organization}/{project}/_apis/wit/wiql?api-version=7.1-preview.2";
        var wiqlContent = new StringContent(JsonSerializer.Serialize(wiqlRequest),
            System.Text.Encoding.UTF8, "application/json");

        var wiqlResponse = await _httpClient.PostAsync(wiqlUrl, wiqlContent);
        var wiqlResult = await wiqlResponse.Content.ReadAsStringAsync();
        var workItemIds = JsonSerializer.Deserialize<WorkItemQueryResult>(wiqlResult, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        });

        if (workItemIds?.WorkItems == null || !workItemIds.WorkItems.Any())
        {
            return (0, 0);
        }

        var totalWorkItems = workItemIds.WorkItems.Count;
        var completedStates = new[] { "Done", "Closed", "Resolved", "Completed" };
        var completed = 0;

        // Processar em lotes
        var batchSize = 200;
        for (int i = 0; i < workItemIds.WorkItems.Count; i += batchSize)
        {
            var batch = workItemIds.WorkItems.Skip(i).Take(batchSize);
            var ids = string.Join(",", batch.Select(wi => wi.Id));

            var detailsUrl = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems" +
                           $"?ids={ids}&fields=System.State&api-version=7.1-preview.3";

            try
            {
                var detailsResponse = await _httpClient.GetStringAsync(detailsUrl);
                var detailsResult = JsonSerializer.Deserialize<WorkItemBatchResult>(detailsResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                if (detailsResult?.Value != null)
                {
                    completed += detailsResult.Value.Count(wi =>
                        wi.Fields?.TryGetValue("System.State", out var state) == true &&
                        completedStates.Contains(state?.ToString(), StringComparer.OrdinalIgnoreCase));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao buscar detalhes do lote de work items");
            }

            await Task.Delay(100);
        }

        return (totalWorkItems, completed);
    }

    private async Task<RepositoryStatistics> GetRepositoryStatsAsync(string organization, string? project)
    {
        try
        {
            var repositories = await GetRepositoriesAsync(organization, project, "");

            // Considera repositório ativo se teve commits nos últimos 30 dias
            var activeCount = 0;
            var cutoffDate = DateTime.Now.AddDays(-30);

            foreach (var repo in repositories.Take(20)) // Limitar para performance
            {
                try
                {
                    var projectName = repo.Project?.Name ?? project ?? "Unknown";
                    var commitsUrl = $"https://dev.azure.com/{organization}/{projectName}/_apis/git/repositories/{repo.Id}/commits" +
                                   $"?searchCriteria.fromDate={cutoffDate:yyyy-MM-ddTHH:mm:ssZ}" +
                                   $"&$top=1&api-version=7.1-preview.1";

                    var commitsResponse = await _httpClient.GetStringAsync(commitsUrl);
                    var commitsResult = JsonSerializer.Deserialize<GitCommitRefListResponse>(commitsResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });

                    if (commitsResult?.Value?.Any() == true)
                    {
                        activeCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao verificar atividade do repositório {RepoId}", repo.Id);
                }

                await Task.Delay(50);
            }

            return new RepositoryStatistics
            {
                TotalRepositories = repositories.Count,
                ActiveRepositories = activeCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar estatísticas de repositórios");
            return new RepositoryStatistics();
        }
    }

    private async Task<double> GetAvgCommitsPerDayAsync(string organization, string? project, int days, string token)
    {
        try
        {
            var commits = await GetCommitsFromApiAsync(organization, project, null, null, null, token);
            return days > 0 ? Math.Round((double)commits.Count / days, 2) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao calcular commits por dia");
            return 0;
        }
    }

    private async Task<int> GetActiveDevelopersCountAsync(string organization, string? project, DateTime fromDate, DateTime toDate, string token)
    {
        try
        {
            var commits = await GetCommitsFromApiAsync(organization, project, fromDate.Year, fromDate.Month, null, token);
            return commits.Select(c => c.Author.Email).Distinct().Count();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao contar desenvolvedores ativos");
            return 0;
        }
    }

    private async Task<double> GetCodeCoverageAsync(string organization, string? project, DateTime fromDate, DateTime toDate)
    {
        try
        {
            var coverageValues = new List<double>();

            if (!string.IsNullOrEmpty(project))
            {
                var coverage = await GetCodeCoverageForProject(organization, project, fromDate, toDate);
                if (coverage > 0) coverageValues.Add(coverage);
            }
            else
            {
                var projects = await GetAllProjectsAsync(organization);
                foreach (var proj in projects.Take(5))
                {
                    try
                    {
                        var coverage = await GetCodeCoverageForProject(organization, proj.Name, fromDate, toDate);
                        if (coverage > 0) coverageValues.Add(coverage);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao buscar cobertura do projeto {ProjectName}", proj.Name);
                    }
                    await Task.Delay(100);
                }
            }

            return coverageValues.Any() ? Math.Round(coverageValues.Average(), 2) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular cobertura de código");
            return 0;
        }
    }

    private async Task<double> GetCodeCoverageForProject(string organization, string project, DateTime fromDate, DateTime toDate)
    {
        var buildsUrl = $"https://dev.azure.com/{organization}/{project}/_apis/build/builds" +
                       $"?minTime={fromDate:yyyy-MM-ddTHH:mm:ssZ}" +
                       $"&maxTime={toDate:yyyy-MM-ddTHH:mm:ssZ}" +
                       $"&resultFilter=succeeded" +
                       $"&$top=10&api-version=7.1-preview.7";

        var buildsResponse = await _httpClient.GetStringAsync(buildsUrl);
        var buildsResult = JsonSerializer.Deserialize<BuildListResponse>(buildsResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        });

        var coverageValues = new List<double>();

        if (buildsResult?.Value != null)
        {
            foreach (var build in buildsResult.Value.Take(5))
            {
                try
                {
                    var coverageUrl = $"https://dev.azure.com/{organization}/{project}/_apis/test/codecoverage" +
                                    $"?buildId={build.Id}&api-version=7.1-preview.1";

                    var coverageResponse = await _httpClient.GetStringAsync(coverageUrl);
                    var coverageResult = JsonSerializer.Deserialize<CodeCoverageResult>(coverageResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });

                    if (coverageResult?.CoverageData?.Any() == true)
                    {
                        var totalLines = coverageResult.CoverageData.Sum(cd => cd.CoverageStats?.Sum(cs => cs.Total) ?? 0);
                        var coveredLines = coverageResult.CoverageData.Sum(cd => cd.CoverageStats?.Sum(cs => cs.Covered) ?? 0);

                        if (totalLines > 0)
                        {
                            coverageValues.Add((double)coveredLines / totalLines * 100);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao buscar cobertura para build {BuildId}", build.Id);
                }
                await Task.Delay(50);
            }
        }

        return coverageValues.Any() ? coverageValues.Average() : 0;
    }
}