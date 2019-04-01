using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using R2A.ReportApi.Service.Model;
using Tester.Model;

namespace R2A.ReportApi.Service.ReportSubmission.Pipelines
{
    public class XmlMetadaValidationBehavior : IPipelineBehavior<ReportSubmissionRequest, Unit>
    {

        public void ValidateHeader(ReportSubmissionRequest request)
        {
            if (request.IsFileValid && request.IsModelValid)
            {
                var submissionInfo = request.SubmissionInfo;
                var xmlMetadataProcessingResult = new List<ValidationRule>();
                var reportData = request.ReportData;

                if (reportData.Data[reportData.ReportConfig.Code] is Dictionary<string, object> rootData)
                {
                    if (rootData[reportData.ReportConfig.HeaderForm.XmlNode] is Dictionary<string, object> headerData)
                    {
                        if (headerData["Year"] == null ||
                            (int.TryParse(headerData["Year"].ToString(), out var year) &&
                             (year != request.ReportingPeriod?.Year)))
                        {
                            xmlMetadataProcessingResult.Add(ValidationRule.Error(ValidationRuleConstant.InvalidXmlHeaderYearErrorCode,
                                $"Year in xml differs from the submitted year in periodInfo. ({headerData["Year"]} <> {request.ReportingPeriod?.Year})"));
                        }


                        if (headerData["Period"] == null ||
                            (int.TryParse(headerData["Period"].ToString(), out var period) &&
                             (period != request.ReportingPeriod?.Period)))
                        {
                            xmlMetadataProcessingResult.Add(ValidationRule.Error(ValidationRuleConstant.InvalidXmlHeaderPeriodErrorCode,
                                $"Report period in xml differs from the submitted period. ({headerData["Period"]} <> {request.ReportingPeriod?.Period})"));
                        }

                        if (headerData["Undertaking"] == null ||
                            headerData["Undertaking"].ToString() != submissionInfo.Undertaking)
                        {
                            xmlMetadataProcessingResult.Add(ValidationRule.Error(ValidationRuleConstant.InvalidXmlHeaderBankErrorCode,
                                $"Report undertaking in xml differs from the submitted undertaking. ({headerData["Undertaking"]} <> {submissionInfo.Undertaking})"));
                        }
                    }
                }
                
                if (xmlMetadataProcessingResult.Any())
                {
                    request.IsReportValid = false;
                    request.ProcessingResult.AddRange(xmlMetadataProcessingResult);
                }                
            }
        }
        public async Task<Unit> Handle(ReportSubmissionRequest request, CancellationToken cancellationToken,
            RequestHandlerDelegate<Unit> next)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateHeader(request);

            return await next();
        }
    }
}