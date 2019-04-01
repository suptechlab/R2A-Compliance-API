using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NLog;
using Pinecone.ReportDataConverter.Extensions.Validator;
using R2A.ReportApi.Service.Infrastructure;
using R2A.ReportApi.Service.Model;
using Tester.Model;

namespace R2A.ReportApi.Service.ReportSubmission.Pipelines
{
    public class DynamicFieldsValidationBehavior : IPipelineBehavior<ReportSubmissionRequest, Unit>
    {
        private readonly Settings _settings;
        private readonly ILogger _logger;


        public DynamicFieldsValidationBehavior(ILogFactory logFactory, Settings settings)
        {
            _settings = settings;
            _logger = logFactory.GetLogger<DynamicFieldsValidationBehavior>();
        }

        public void ProcessDynamicFields(ReportSubmissionRequest request)
        {
            if (request.IsModelValid && request.ReportData != null)
            {
                var dynamicFieldValidationResult = request.ReportData.ValidateReportSpecialFields();
                if (!dynamicFieldValidationResult.IsValid)
                {
                    //TODO: Odluciti da li je MODEL (XSD) validan pa makar dinamicka polja nisu dobra
                    //request.IsModelValid = false;
                    request.IsReportValid = false;

                    dynamicFieldValidationResult.ValidationErrors.ToList().ForEach(err =>
                    {
                        if (err.References == null || err.References.Count != 1 || string.IsNullOrEmpty(err.Value))
                        {
                            request.ProcessingResult.Add(
                                ValidationRule.Error(
                                    ValidationRuleConstant.DynamicFieldError,
                                    err.ToString()));
                        }
                        else
                        {
                            var refString = err.References[0].ToString();
                            request.ProcessingResult.Add(
                                ValidationRule.Error(
                                    ValidationRuleConstant.DynamicFieldError,
                                    $"Invalid value for dynamic ENUM field ({refString} = {err.Value})"));
                        }
                    });
                }
            }
        }

        public async Task<Unit> Handle(ReportSubmissionRequest request, CancellationToken cancellationToken,
            RequestHandlerDelegate<Unit> next)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProcessDynamicFields(request);
            return await next();
        }
    }
}