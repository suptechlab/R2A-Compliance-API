using Tester.Model;

namespace R2A.ReportApi.Service.Model
{
    public class ValidationRule
    {
        public static ValidationRule Error(ValidationRuleConstant validationInfo)
        {
            return Create(validationInfo.Code, validationInfo.Description, null, null, null, ValidationRuleSeverity.Error);
        }

        public static ValidationRule Error(string validationId, string description)
        {
            return Create(validationId, description, null, null, null, ValidationRuleSeverity.Error);
        }

        public static ValidationRule Error(string validationCode, int validationNumber, string description)
        {
            return Create($"{validationCode}-{validationNumber}", description, null, null, null,
                ValidationRuleSeverity.Error);
        }

        public static ValidationRule Error(string validationId, string description, string formula,
            string formulaResult)
        {
            return Create(validationId, description, null, formula, formulaResult, ValidationRuleSeverity.Error);
        }

        public static ValidationRule Error(string validationCode, int validationNumber, string description,
            string formula, string formulaResult)
        {
            return Create($"{validationCode}-{validationNumber}", description, null, formula, formulaResult,
                ValidationRuleSeverity.Error);
        }

        public static ValidationRule Warning(ValidationRuleConstant validationInfo)
        {
            return Create(validationInfo.Code, validationInfo.Description, null, null, null, ValidationRuleSeverity.Warning);
        }

        public static ValidationRule Warning(string validationId, string description)
        {
            return Create(validationId, description, null, null, null, ValidationRuleSeverity.Warning);
        }

        public static ValidationRule Warning(string validationCode, int validationNumber, string description)
        {
            return Create($"{validationCode}-{validationNumber}", description, null, null, null,
                ValidationRuleSeverity.Warning);
        }

        public static ValidationRule Warning(string validationId, string description, string formula,
            string formulaResult)
        {
            return Create(validationId, description, null, formula, formulaResult, ValidationRuleSeverity.Warning);
        }

        public static ValidationRule Warning(string validationCode, int validationNumber, string description,
            string formula, string formulaResult)
        {
            return Create($"{validationCode}-{validationNumber}", description, null, formula, formulaResult,
                ValidationRuleSeverity.Warning);
        }


        public static ValidationRule Create(string validationId, string description, string additionalDescription, string formula,
            string formulaResult, ValidationRuleSeverity severity)
        {
            return new ValidationRule
            {
                ValidationId = validationId,
                Description = description,
                AdditionalDescription = additionalDescription,
                Formula = formula,
                FormulaResult = formulaResult,
                Severity = severity
            };
        }


        public string ValidationId { get; set; }
        public string Description { get; set; }
        public string AdditionalDescription { get; set; }
        public string Formula { get; set; }
        public string FormulaResult { get; set; }
        public string FormulaSource { get; set; }
        public ValidationRuleSeverity Severity { get; set; }
        public string FormulaDescription { get; set; }
    }
}