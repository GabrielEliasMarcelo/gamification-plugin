using System.ComponentModel.DataAnnotations;

namespace AzureDevOps.Gamification.Api.DTOs
{
    /// <summary>
    /// DTOs com validação aprimorada
    /// </summary>
    public record ValidatedMetricsRequest
    {
        [Required(ErrorMessage = "Organization é obrigatório")]
        [ValidAzureDevOpsOrg]
        public string Organization { get; init; } = "";

        [Required(ErrorMessage = "Project é obrigatório")]
        [StringLength(100, ErrorMessage = "Nome do projeto muito longo")]
        public string Project { get; init; } = "";

        [Range(2020, 2030, ErrorMessage = "Ano deve estar entre 2020 e 2030")]
        public int? Year { get; init; }

        [Range(1, 12, ErrorMessage = "Mês deve estar entre 1 e 12")]
        public int? Month { get; init; }

        [StringLength(100, ErrorMessage = "Nome do autor muito longo")]
        public string? Author { get; init; }
    }
}