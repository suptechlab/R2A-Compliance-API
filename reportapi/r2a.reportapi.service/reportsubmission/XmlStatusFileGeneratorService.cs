using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using R2A.ReportApi.Service.Infrastructure;
using R2A.ReportApi.Service.Model;
using Tester.Model;

namespace R2A.ReportApi.Service.ReportSubmission
{
    public static class XmlStatusFileGeneratorService
    {
        private const string SchemaVersion = "http://bsp.gov.ph/SubmissionResult/v01";

        public static void GenerateFileAsync(ReportSubmissionRequest request, string filePath, DateTime processingEndTime)
        {
            using (var writer = new XmlTextWriter(filePath, Encoding.UTF8))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartDocument();

                writer.WriteStartElement("SubmissionResult", SchemaVersion);
                writer.WriteStartElement("SubmissionInfo");
                writer.WriteStartElement("SubmissionToken");
                writer.WriteValue(request.Token.ToString());
                writer.WriteEndElement();
                writer.WriteStartElement("ReportCode");
                writer.WriteValue(request.SubmissionInfo.ReportCode);
                writer.WriteEndElement();
                writer.WriteStartElement("ReportPeriod");
                writer.WriteValue(request.SubmissionInfo.ReportPeriod);
                writer.WriteEndElement();
                writer.WriteStartElement("Undertaking");
                writer.WriteValue(request.SubmissionInfo.Undertaking);
                writer.WriteEndElement();
                writer.WriteStartElement("SubmissionTime");
                writer.WriteValue(request.TimeSubmitted);
                writer.WriteEndElement();
                writer.WriteStartElement("ResponseTime");
                writer.WriteValue(processingEndTime);
                writer.WriteEndElement();
                writer.WriteStartElement("SubmissionStatus");
                writer.WriteValue(request.SubmissionInfo.SubmissionStatus == SubmittedReportStatus.Accepted
                    ? "ACPT"
                    : "RJCT");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteStartElement("ProcessingResult");
                foreach (var validationRule in request.ProcessingResult)
                {
                    writer.WriteStartElement("ValidationRule");
                    writer.WriteStartElement("Id");
                    writer.WriteValue(validationRule.ValidationId);
                    writer.WriteEndElement();
                    writer.WriteStartElement("Desc");
                    writer.WriteValue(validationRule.Description + (string.IsNullOrWhiteSpace(validationRule.AdditionalDescription) ? "" : " - " + validationRule.AdditionalDescription));
                    writer.WriteEndElement();
                    
                    if (!string.IsNullOrEmpty(validationRule.Formula))
                    {
                        writer.WriteStartElement("Formula");
                        writer.WriteValue(validationRule.Formula);
                        writer.WriteEndElement();
                    }
                    if (!string.IsNullOrEmpty(validationRule.FormulaDescription))
                    {
                        writer.WriteStartElement("FormulaDesc");
                        writer.WriteValue(validationRule.FormulaDescription);
                        writer.WriteEndElement();
                    }
                    if (!string.IsNullOrEmpty(validationRule.FormulaResult))
                    {
                        writer.WriteStartElement("FormulaResult");
                        writer.WriteValue(validationRule.FormulaResult);
                        writer.WriteEndElement();
                    }

                    writer.WriteStartElement("Severity");
                    writer.WriteValue(validationRule.Severity == ValidationRuleSeverity.Warning ? "WARN" : "ERR");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteEndDocument();
            }
        }
    }
}
