using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AzureDevOps.Gamification.Api.Middleware
{
    /// <summary>
    /// Health check customizado para verificar conectividade com Azure DevOps
    /// </summary>
    public class AzureDevOpsHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AzureDevOpsHealthCheck> _logger;

        public AzureDevOpsHealthCheck(HttpClient httpClient, ILogger<AzureDevOpsHealthCheck> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Testa conectividade básica com Azure DevOps (endpoint público)
                var response = await _httpClient.GetAsync(
                    "https://dev.azure.com/_apis/resourceareas",
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("Azure DevOps API is accessible");
                }

                _logger.LogWarning("Azure DevOps API returned status code: {StatusCode}", response.StatusCode);
                return HealthCheckResult.Degraded($"Azure DevOps API returned {response.StatusCode}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to connect to Azure DevOps API");
                return HealthCheckResult.Unhealthy("Cannot connect to Azure DevOps API", ex);
            }
            catch (TaskCanceledException)
            {
                return HealthCheckResult.Unhealthy("Timeout connecting to Azure DevOps API");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Azure DevOps health check");
                return HealthCheckResult.Unhealthy("Unexpected error during health check", ex);
            }
        }
    }
}