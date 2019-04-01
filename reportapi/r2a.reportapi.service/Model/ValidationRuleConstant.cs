namespace R2A.ReportApi.Service.Model
{
    public class ValidationRuleConstant
    {
        
        public static readonly ValidationRuleConstant Base64DecodeError = new ValidationRuleConstant("DATA-0001", "Submitted report data is not valid BASE64 encoded data");
        public static readonly ValidationRuleConstant ZipEntryCountError = new ValidationRuleConstant("DATA-0002", "ZIPed report data does not contain exactly one entry");
        public static readonly ValidationRuleConstant ZipCorruptedArchiveError = new ValidationRuleConstant("DATA-0003", "ZIPed report data archive cannot be extracted");

        public const string BankNotSpecifiedErrorCode = "META-0001";
        public const string BankNotFoundErrorCode = "META-0002";
        public const string BankNotAllowedErrorCode = "META-0003";
        public const string ReportNotFoundErrorCode = "META-0004";
        public const string ReportPeriodFormatInvalidErrorCode = "META-0005";
        public const string ReportVersionNotFoundErrorCode = "META-0006";

        public const string FormulaExceptionError = "VAL-ERR";

        public const string XsdValidatorErrorCode = "XSD-0001";
        public const string XsdValidatorWarningCode = "XSD-0002";
        public const string InvalidRootTagErrorCode = "XSD-0003";
        public const string InvalidRootNamespaceErrorCode = "XSD-0004";
        public const string InvalidXmlStructure = "XSD-0005";

        public const string InvalidXmlHeaderYearErrorCode = "XML-0001";
        public const string InvalidXmlHeaderPeriodErrorCode = "XML-0002";
        public const string InvalidXmlHeaderBankErrorCode = "XML-0003";
        public const string DynamicFieldError = "DYN-0001";

        public const string TemplateNotIncluded = "REP-0001";

        public string Code { get; }
        public string Description { get; }

        public ValidationRuleConstant(string code, string description)
        {
            Code = code;
            Description = description;
        }
    }
}