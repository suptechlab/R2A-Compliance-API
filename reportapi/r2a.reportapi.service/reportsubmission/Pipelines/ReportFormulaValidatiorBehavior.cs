using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSP.ReportDataConverter.Implementations;
using BSP.ReportDataConverter.Implementations.Warehouse.Lookup;
using MediatR;
using NLog;
using Pinecone.ReportDataConverter.Extensions.Misc;
using Pinecone.ReportFormula.Interpreter;
using Pinecone.ReportFormula.Interpreter.Lookup;
using R2A.ReportApi.Service.Infrastructure;
using R2A.ReportApi.Service.Model;
using R2A.ReportApi.Service.ReportSubmission.ReportConfiguration;
using Tester.Model;

namespace R2A.ReportApi.Service.ReportSubmission.Pipelines
{
    public class ReportFormulaValidatiorBehavior : IPipelineBehavior<ReportSubmissionRequest, Unit>
    {
        private readonly ILogger _logger;
        private readonly Settings _settings;
        private readonly ILookupResolver _lookupResolver;
        private readonly CultureInfo _cultureInfo;

        public ReportFormulaValidatiorBehavior(ILogFactory logFactory, Settings settings, ILookupResolver lookupResolver, CultureInfo cultureInfo)
        {
            _settings = settings;
            _logger = logFactory.GetLogger<ReportFormulaValidatiorBehavior>();
            _lookupResolver = lookupResolver;
            _cultureInfo = cultureInfo;
        }

        public void ValidateReport(ReportSubmissionRequest request, Settings settings)
        {
            if (!request.ReportVersionId.HasValue)
            {
                request.IsReportValid = false;
                return;
            }

            var reportDefinitionData =
                ReportDefinitionCacheManager.GetConfiguration(request.ReportVersionId.Value, settings, _cultureInfo);
            if (reportDefinitionData != null)
            {
                var rd = request.ReportData;

                ExpandReportForms(request);
                
                var cm = rd.CreateContextManager();
                cm.DwhsLookup = new DwhsLookup(
                    reportDefinitionData.ReportCode, 
                    request.SubmissionInfo.Undertaking,
                    reportDefinitionData.RecurrenceType,
                    request.ReportingPeriod.Year.Value,
                    request.ReportingPeriod.Period.Value,
                    settings.DbConnectionString,
                    reportDefinitionData.FormWarehouseConfigs
                );
                cm.LookupResolver = _lookupResolver;
                reportDefinitionData.ValidationFormulas.ForEach(validation =>
                {
                    if (validation.IsValid && validation.Active)
                    {
                        var isApplicable =
                            (
                                validation.RequiredTemplatesLeft.Count == 0 ||
                                validation.RequiredTemplatesLeft.Any(template => rd.ReportedForms.Contains(template))
                            ) &&
                            (
                                validation.RequiredTemplatesRight.Count == 0 ||
                                validation.RequiredTemplatesRight.Any(template => rd.ReportedForms.Contains(template))
                            );
                        if (isApplicable && validation.ConditionFormulaNode != null)
                        {
                            var conditionValue = AstNodeInterpreter.InterpretNode(validation.ConditionFormulaNode, cm);
                            if (conditionValue == null || conditionValue.IsNull() || !conditionValue.GetBooleanValue().HasValue)
                            {
                                _logger.Error($"Unexpected result evaluating formula condition ({validation.Code})");
                            } else
                            {
                                var booleanValue = conditionValue.GetBooleanValue();
                                isApplicable = booleanValue.HasValue && booleanValue.Value;
                            }
                        }
                        
                        if (isApplicable)
                        {
                            var leftValue = AstNodeInterpreter.InterpretNode(validation.LeftFormulaNode, cm);
                            var rightValue = AstNodeInterpreter.InterpretNode(validation.RightFormulaNode, cm);
                            bool isValid;
                            if (leftValue.IsNull() || rightValue.IsNull())
                            {
                                _logger.Warn($"Formula result is null! ReportId {request.Id}, ValidationId {validation.Id}, " +
                                             $"LeftResult {(leftValue.IsNull()?"null":ReportValidationFormula.GetSingleResultString(leftValue,_cultureInfo))}, " +
                                             $"RightResult {(rightValue.IsNull() ? "null" : ReportValidationFormula.GetSingleResultString(rightValue,_cultureInfo))}");
                                isValid = false;
                            }
                            else
                            {
                                isValid = validation.Tolerance.HasValue
                                    ? leftValue.CompareWith(rightValue, validation.OperatorString, 1,
                                        validation.Tolerance.Value)
                                    : leftValue.CompareWith(rightValue, validation.OperatorString);
                            }
                            //Console.WriteLine($"{validation.Code}|{leftValue}|{validation.OperatorString}|{rightValue}");
                            if (!isValid)
                            {
                                var valErr = ValidationRule.Create(validation.Code,
                                    validation.Description, validation.AdditionalDescription, validation.FormulaString,
                                    validation.GetFormulaResultString(leftValue, rightValue, _cultureInfo),
                                    validation.Severity);
                                valErr.FormulaSource = validation.GetFormulaSourceString();
                                valErr.FormulaDescription = validation.UserFriendlyFormula;
                                
                                request.ProcessingResult.Add(valErr);
                                if (validation.Severity == ValidationRuleSeverity.Error)
                                {
                                    request.IsReportValid = false;
                                }
                            }
                        }
                    }
                });
            }
        }

        private void ExpandReportForms(ReportSubmissionRequest request)
        {
            if (request.IsModelValid && request.ReportingPeriod != null && request.ReportingPeriod.Period.HasValue)
            {
                var bspTemplateRequirementChecker = new BspTemplateRequirementChecker(
                    request.SubmissionInfo.Undertaking, request.SubmissionInfo.ReportCode,
                    request.ReportingPeriod.Period.Value, _settings.DbConnectionString);

                request.ReportData.ExpandAllForms(bspTemplateRequirementChecker);
            }
        }

        public async Task<Unit> Handle(ReportSubmissionRequest request, CancellationToken cancellationToken,
            RequestHandlerDelegate<Unit> next)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (request.IsModelValid && request.ReportData != null)
            {
                try
                {
                    ValidateReport(request, _settings);
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"An error occured during formula validation. ReportStatus Id: {request.Id}.");
                    throw;
                }
            }
            else
            {
                request.ProcessingResult.Add(ValidationRule.Error(ValidationRuleConstant.FormulaExceptionError,
                    "The system was unable to run formula validations because the report file itself is not valid."));
                request.IsReportValid = false;
            }

            return await next();
        }
    }
}