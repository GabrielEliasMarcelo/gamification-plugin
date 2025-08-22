using AzureDevOps.Gamification.Api.Services;

namespace AzureDevOps.Gamification.Api.Extensions
{
    /// <summary>
    /// Extension methods para configuração adicional
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adiciona todos os serviços relacionados ao Azure DevOps
        /// </summary>
        public static IServiceCollection AddAzureDevOpsServices(this IServiceCollection services)
        {
            services.AddHttpClient<IAzureDevOpsService, AzureDevOpsService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "AzureDevOps-Gamification/1.0");
            });

            services.AddScoped<IAzureDevOpsService, AzureDevOpsService>();

            return services;
        }

        /// <summary>
        /// Configura cache otimizado para o domínio da aplicação
        /// </summary>
        public static IServiceCollection AddGamificationCache(this IServiceCollection services)
        {
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = 100;
                options.CompactionPercentage = 0.25; // Remove 25% quando atinge limite
            });

            return services;
        }
    }
}