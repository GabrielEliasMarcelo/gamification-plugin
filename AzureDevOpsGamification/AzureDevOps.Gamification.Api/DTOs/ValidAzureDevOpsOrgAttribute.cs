using System.ComponentModel.DataAnnotations;

namespace AzureDevOps.Gamification.Api.DTOs
{
    /// <summary>
    /// Atributo para validação de organizações válidas do Azure DevOps
    /// </summary>
    public class ValidAzureDevOpsOrgAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is not string orgName || string.IsNullOrEmpty(orgName))
                return false;

            // Validação básica: sem espaços, caracteres especiais limitados
            return orgName.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')
                   && orgName.Length <= 50;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"'{name}' deve ser um nome válido de organização do Azure DevOps";
        }
    }
}