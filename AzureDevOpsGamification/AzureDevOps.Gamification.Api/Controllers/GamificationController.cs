using AzureDevOps.Gamification.Api.DTOs;
using AzureDevOps.Gamification.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureDevOps.Gamification.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamificationController : ControllerBase
{
    private readonly IAzureDevOpsService _azureDevOpsService;
    private readonly ILogger<GamificationController> _logger;

    public GamificationController(
        IAzureDevOpsService azureDevOpsService,
        ILogger<GamificationController> logger)
    {
        _azureDevOpsService = azureDevOpsService;
        _logger = logger;
    }

    /// <summary>
    /// Obtém métricas de commits filtradas por parâmetros
    /// </summary>
    /// <param name="request">Parâmetros de filtro para métricas</param>
    /// <returns>Métricas agregadas de commits</returns>
    [HttpGet("metrics/commits")]
    [ProducesResponseType(typeof(CommitMetrics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CommitMetrics>> GetCommitMetrics([FromQuery] MetricsRequest request)
    {
        // Validação de entrada - apenas organização é obrigatória
        if (string.IsNullOrEmpty(request.Organization))
        {
            return BadRequest("Organization é obrigatório");
        }

        var token = ExtractAzureDevOpsToken(request.Token);
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("Token do Azure DevOps é obrigatório. Use o parâmetro 'token' ou header 'X-Azure-DevOps-Token'");
        }

        try
        {
            var scope = string.IsNullOrEmpty(request.Project) ? "toda organização" : $"projeto {request.Project}";
            _logger.LogInformation("Buscando métricas de commits para {Organization} - {Scope}",
                request.Organization, scope);

            var metrics = await _azureDevOpsService.GetCommitMetricsAsync(
                request.Organization,
                request.Project,
                request.Year,
                request.Month,
                request.Author,
                token);

            return Ok(metrics);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("Token inválido ou sem permissões para acessar Azure DevOps");
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("401"))
        {
            return Unauthorized("Token do Azure DevOps inválido ou expirado");
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("403"))
        {
            return StatusCode(403, "Token sem permissões necessárias para acessar os dados solicitados");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro ao acessar Azure DevOps API");
            return StatusCode(502, "Erro ao comunicar com Azure DevOps");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro interno ao processar métricas");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obtém ranking dos top desenvolvedores baseado em score calculado
    /// </summary>
    /// <param name="request">Parâmetros de filtro para ranking</param>
    /// <returns>Lista dos top 10 desenvolvedores ordenados por score</returns>
    [HttpGet("ranking/developers")]
    [ProducesResponseType(typeof(IEnumerable<DeveloperRanking>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<DeveloperRanking>>> GetDeveloperRanking([FromQuery] RankingRequest request)
    {
        if (string.IsNullOrEmpty(request.Organization))
        {
            return BadRequest("Organization é obrigatório");
        }

        var token = ExtractAzureDevOpsToken(request.Token);
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("Token do Azure DevOps é obrigatório. Use o parâmetro 'token' ou header 'X-Azure-DevOps-Token'");
        }

        try
        {
            var scope = string.IsNullOrEmpty(request.Project) ? "toda organização" : $"projeto {request.Project}";
            _logger.LogInformation("Calculando ranking de desenvolvedores para {Organization} - {Scope}",
                request.Organization, scope);

            var ranking = await _azureDevOpsService.GetDeveloperRankingAsync(
                request.Organization,
                request.Project, // Agora pode ser null
                request.Year,
                request.Month,
                token);

            // Retorna apenas top 10 para performance
            return Ok(ranking.Take(10));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("Token do Azure DevOps inválido ou sem permissões");
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("401"))
        {
            return Unauthorized("Token do Azure DevOps inválido ou expirado");
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("403"))
        {
            return StatusCode(403, "Token sem permissões necessárias para acessar os dados solicitados");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular ranking de desenvolvedores");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    private string? ExtractAzureDevOpsToken(object token)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtém dados para construção do CodeGraph de colaboração
    /// </summary>
    /// <param name="request">Parâmetros para geração do grafo</param>
    /// <returns>Dados estruturados para visualização do grafo</returns>
    [HttpGet("codegraph")]
    [ProducesResponseType(typeof(CodeGraphData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CodeGraphData>> GetCodeGraph([FromQuery] CodeGraphRequest request)
    {
        if (string.IsNullOrEmpty(request.Organization))
        {
            return BadRequest("Organization é obrigatório");
        }

        var token = ExtractAzureDevOpsToken(request.Token);
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("Token do Azure DevOps é obrigatório. Use o parâmetro 'token' ou header 'X-Azure-DevOps-Token'");
        }

        try
        {
            var scope = string.IsNullOrEmpty(request.Project) ? "toda organização" : $"projeto {request.Project}";
            _logger.LogInformation("Gerando CodeGraph para {Organization} - {Scope}",
                request.Organization, scope);

            var graphData = await _azureDevOpsService.GetCodeGraphDataAsync(
                request.Organization,
                request.Project, // Agora pode ser null
                request.RepositoryId,
                token);

            return Ok(graphData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar dados do CodeGraph");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obtém estatísticas gerais de builds e trabalho
    /// </summary>
    /// <param name="request">Parâmetros de filtro</param>
    /// <returns>Estatísticas agregadas do projeto ou organização</returns>
    [HttpGet("stats/general")]
    [ProducesResponseType(typeof(GeneralStats), StatusCodes.Status200OK)]
    public async Task<ActionResult<GeneralStats>> GetGeneralStats([FromQuery] StatsRequest request)
    {
        if (string.IsNullOrEmpty(request.Organization))
        {
            return BadRequest("Organization é obrigatório");
        }

        var token = ExtractAzureDevOpsToken(request.Token);
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("Token do Azure DevOps é obrigatório. Use o parâmetro 'token' ou header 'X-Azure-DevOps-Token'");
        }

        try
        {
            var scope = string.IsNullOrEmpty(request.Project) ? "toda organização" : $"projeto {request.Project}";
            _logger.LogInformation("Obtendo estatísticas gerais para {Organization} - {Scope}",
                request.Organization, scope);

            var stats = await _azureDevOpsService.GetGeneralStatsAsync(
                request.Organization,
                request.Project, // Agora pode ser null
                request.Days ?? 30,
                token);

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter estatísticas gerais");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Obtém análise detalhada dos commits incluindo tamanho e qualidade
    /// </summary>
    /// <param name="request">Parâmetros de filtro para análise</param>
    /// <returns>Análise detalhada dos commits com informações de tamanho e qualidade</returns>
    [HttpGet("commits/analysis")]
    [ProducesResponseType(typeof(CommitAnalysis), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CommitAnalysis>> GetCommitAnalysis([FromQuery] CommitAnalysisRequest request)
    {
        if (string.IsNullOrEmpty(request.Organization))
        {
            return BadRequest("Organization é obrigatório");
        }

        var token = ExtractAzureDevOpsToken(request.Token);
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("Token do Azure DevOps é obrigatório. Use o parâmetro 'token' ou header 'X-Azure-DevOps-Token'");
        }

        try
        {
            var scope = string.IsNullOrEmpty(request.Project) ? "toda organização" : $"projeto {request.Project}";
            _logger.LogInformation("Analisando commits detalhadamente para {Organization} - {Scope}",
                request.Organization, scope);

            var analysis = await _azureDevOpsService.GetCommitAnalysisAsync(
                request.Organization,
                request.Project,
                request.Year,
                request.Month,
                request.Author,
                request.RepositoryId,
                token);

            return Ok(analysis);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("Token do Azure DevOps inválido ou sem permissões");
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("401"))
        {
            return Unauthorized("Token do Azure DevOps inválido ou expirado");
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("403"))
        {
            return StatusCode(403, "Token sem permissões necessárias para acessar os dados solicitados");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao analisar commits");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Extrai token do Azure DevOps do parâmetro de query ou header
    /// </summary>
    private string? ExtractAzureDevOpsToken(string? queryToken = null)
    {
        // Prioridade: 1. Query parameter, 2. Header customizado, 3. Authorization header
        if (!string.IsNullOrEmpty(queryToken))
            return queryToken;

        // Header customizado para Azure DevOps
        var headerToken = Request.Headers["X-Azure-DevOps-Token"].FirstOrDefault();
        if (!string.IsNullOrEmpty(headerToken))
            return headerToken;

        // Authorization header (para compatibilidade)
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader))
        {
            var parts = authHeader.Split(' ');
            if (parts.Length == 2 && (parts[0].Equals("Bearer", StringComparison.OrdinalIgnoreCase) ||
                                     parts[0].Equals("Token", StringComparison.OrdinalIgnoreCase)))
            {
                return parts[1];
            }
        }

        return null;
    }
}