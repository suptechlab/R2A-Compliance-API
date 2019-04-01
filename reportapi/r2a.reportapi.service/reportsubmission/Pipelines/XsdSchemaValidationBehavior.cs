using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using MediatR;
using NLog;
using Pinecone.ReportDataConverter;
using Pinecone.ReportDataConverter.Config.Interfaces;
using Pinecone.ReportDataConverter.Extensions.Xml;
using R2A.ReportApi.Service.Infrastructure;
using R2A.ReportApi.Service.Model;
using R2A.ReportApi.Service.ReportSubmission.ReportConfiguration;
using Tester.Model;

namespace R2A.ReportApi.Service.ReportSubmission.Pipelines
{
    public class XsdSchemaValidationBehavior : IPipelineBehavior<ReportSubmissionRequest, Unit>
    {
        private readonly Settings _settings;
        private readonly ILogger _logger;
        private readonly IDynamicDropdownDataResolver _dynamicDropdownDataResolver;
        private readonly CultureInfo _cultureInfo;


        public XsdSchemaValidationBehavior(ILogFactory logFactory, Settings settings, IDynamicDropdownDataResolver dynamicDropdownDataResolver, CultureInfo cultureInfo)
        {
            _settings = settings;
            _logger = logFactory.GetLogger<XsdSchemaValidationBehavior>();
            _dynamicDropdownDataResolver = dynamicDropdownDataResolver;
            _cultureInfo = cultureInfo;
        }

        public void ProcessRequest(ReportSubmissionRequest request)
        {
            if (request.ReportVersionId.HasValue)
            {
                var reportDefinitionData =
                    ReportDefinitionCacheManager.GetConfiguration(request.ReportVersionId.Value, _settings, _cultureInfo);
                if (reportDefinitionData != null)
                {
                    var reportData = new ReportData(reportDefinitionData.ReportConfig);
                    try
                    {
                        using (var memStream = new MemoryStream(request.XmlFileContents))
                        {
                            request.IsModelValid = true;
                            var initialErrorCount = request.ProcessingResult.Count;
                            reportData.LoadFromXmlV2(memStream, reportDefinitionData.XmlSchemaSet,
                                (sender, e) =>
                                {
                                    if (e.Exception.LineNumber == 0 && e.Exception.LinePosition == 0 &&
                                        request.ProcessingResult.Count > initialErrorCount)
                                    {
                                        return;
                                    }

                                    var message = e.Message;
                                    var severity = e.Severity;

                                    request.ProcessingResult.Add(
                                        new ValidationRule
                                        {
                                            ValidationId =
                                                severity == XmlSeverityType.Warning
                                                    ? ValidationRuleConstant.XsdValidatorWarningCode
                                                    : ValidationRuleConstant.XsdValidatorErrorCode,
                                            Description =
                                                $"{message} [{e.Exception.LineNumber}:{e.Exception.LinePosition}]",
                                            Formula = null,
                                            FormulaResult = null,
                                            Severity = severity == XmlSeverityType.Warning
                                                ? ValidationRuleSeverity.Warning
                                                : ValidationRuleSeverity.Error
                                        }
                                    );
                                    if (e.Severity == XmlSeverityType.Error)
                                    {
                                        request.IsModelValid = false;
                                    }
                                }
                            );
                        }

                        var reportDataDictionary =
                            (Dictionary<string, object>) reportData.Data[reportData.ReportConfig.XmlNode];
                        request.IsViewable = reportDataDictionary.Count > 0;
                        request.ReportData = reportData;

                        request.ReportData.DynamicDropdownDataResolver = _dynamicDropdownDataResolver;

                        if (reportData.XmlRootNode != reportData.ReportConfig.EscapedXmlNode)
                        {
                            request.ProcessingResult.Add(
                                ValidationRule.Error(ValidationRuleConstant.InvalidRootTagErrorCode,
                                    $"Invalid ROOT element tag [Found '{reportData.XmlRootNode}' but expected '{reportData.ReportConfig.XmlNode}']")
                            );
                            request.IsModelValid = false;
                        }
                        else
                        {
                            var rootNodeNamespace = string.IsNullOrWhiteSpace(reportData.XmlRootNodeNamespace)
                                ? string.Empty
                                : reportData.XmlRootNodeNamespace;
                            var expectedNamespace = string.IsNullOrWhiteSpace(reportData.ReportConfig.XmlNamespace)
                                ? string.Empty
                                : reportData.ReportConfig.XmlNamespace;
                            if (rootNodeNamespace != expectedNamespace)
                            {
                                request.ProcessingResult.Add(
                                    ValidationRule.Error(ValidationRuleConstant.InvalidRootNamespaceErrorCode,
                                        $"Invalid ROOT element namespace [Found '{rootNodeNamespace}' but expected '{expectedNamespace}']")
                                );
                                request.IsModelValid = false;
                            }
                        }
                    }
                    catch (XmlException e)
                    {
                        request.ProcessingResult.Add(
                            ValidationRule.Error(ValidationRuleConstant.InvalidXmlStructure,
                                $"Error parsing XML file - {e.Message}")
                        );
                        request.IsModelValid = false;
                        request.ReportData = null;
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, $"An error occured during XSD validation. ReportStatus Id: {request.Id}.");
                        throw;
                    }
                }
            }
        }

        public async Task<Unit> Handle(ReportSubmissionRequest request, CancellationToken cancellationToken,
            RequestHandlerDelegate<Unit> next)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (request.IsFileValid)
            {
                ProcessRequest(request);
            }

            return await next();
        }
    }
}