using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSP.ReportDataConverter.Implementations.Warehouse.Engine;
using Dapper;
using MediatR;
using NLog;
using Pinecone.ReportDataConverter;
using Pinecone.ReportDataConverter.Extensions.Excel;
using R2A.ReportApi.Models;
using R2A.ReportApi.PdfGenerator;
using R2A.ReportApi.Service.Infrastructure;
using R2A.ReportApi.Service.Model;
using R2A.ReportApi.Service.ReportSubmission.ReportConfiguration;
using Tester.Model;


namespace R2A.ReportApi.Service.ReportSubmission
{
    public class ReportSubmissionHandler : IRequestHandler<ReportSubmissionRequest>
    {
        private readonly Settings _settings;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly PdfStatusFileGeneratorService _pdfGenerator;
        private readonly CultureInfo _cultureInfo;

        public ReportSubmissionHandler(ILogFactory logFactory, Settings settings, IMediator _mediator,
            CultureInfo cultureInfo)
        {
            _settings = settings;
            this._mediator = _mediator;
            _logger = logFactory.GetLogger<ReportSubmissionHandler>();
            _pdfGenerator = new PdfStatusFileGeneratorService(_settings.LogoImagePath);
            _cultureInfo = cultureInfo;
        }


        private void GenerateExcelFile(ReportData reportData, int reportStatusId, string excelPath)
        {
            if (!File.Exists(excelPath))
            {
                try
                {
                    reportData.SaveToExcel(new FileInfo(excelPath));
                    using (var connection = new SqlConnection(_settings.DbConnectionString))
                    {
                        connection.Open();
                        using (var transaction = connection.BeginTransaction())
                        {
                            connection.Execute(new CommandDefinition(
                                "UPDATE dbo.SubmittedReport SET ExcelReportFilePath = @excelPath WHERE ReportStatusId = @reportStatusId;"
                                , new {reportStatusId, excelPath}, transaction));
                            connection.Execute(new CommandDefinition(
                                "UPDATE dbo.ReportStatus SET ExcelReportFilePath = @excelPath WHERE Id = @reportStatusId;"
                                , new { reportStatusId, excelPath}, transaction));
                            transaction.Commit();
                        }
                    }
                }
                catch (Exception e)
                {
                    try
                    {
                        if (File.Exists(excelPath))
                            File.Delete(excelPath);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    _logger.Error(e, "Error during excel file generation.");
                }
            }
        }

        public async Task Handle(ReportSubmissionRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fileNameBase = FileNamingConvention.GenerateFileNameBase(request.SubmissionInfo.ReportCode,
                request.SubmissionInfo.Undertaking,
                request.SubmissionInfo.ReportPeriod, request.TimeSubmitted, request.Id);

            StatusCode submissionStatus, dataProcessingStatus = DataProcessingStatus.NotApplicable;
            if (request.IsModelValid && request.IsFileValid && request.IsReportValid)
            {
                submissionStatus = SubmissionStatus.Accepted;
                request.SubmissionInfo.SubmissionStatus = SubmittedReportStatus.Accepted;
            }
            else
            {
                submissionStatus = SubmissionStatus.Rejected;
                request.SubmissionInfo.SubmissionStatus = SubmittedReportStatus.Rejected;
            }


            string xmlPath = Path.Combine(_settings.XmlFileSaveLocation, fileNameBase + ".xml");
            string excelPath = Path.Combine(_settings.XmlFileSaveLocation, fileNameBase + ".xlsx");
            string xmlStatusFilePath = Path.Combine(_settings.ReportFileSaveLocation, fileNameBase + "_status.xml");
            string pdfStatusFilePath = Path.Combine(_settings.ReportFileSaveLocation, fileNameBase + "_status.pdf");
            try
            {
                var bankInfo = (await _mediator.Send(new GetBankInfoFromCode.Query(request.SubmissionInfo.Undertaking),
                    cancellationToken)).BankInfo;
                using (Stream pdfFile = new FileStream(pdfStatusFilePath, FileMode.Create))
                {
                    _pdfGenerator.GenerateReportSubmitionConfirmation(bankInfo.Title,
                        request.TimeSubmitted,
                        request.SubmissionInfo.ReportCode, request.SubmissionInfo.ReportPeriod,
                        submissionStatus.ToString() + (dataProcessingStatus == DataProcessingStatus.NotApplicable
                            ? ""
                            : $", {dataProcessingStatus}"),
                        request.ProcessingResult.Select(pr =>
                        {
                            var sdi = new StatusDescriptionItem()
                            {
                                Header = pr.ValidationId,
                                HeaderAdditionalInfo = pr.AdditionalDescription,
                                Description = pr.Description,
                                Details = new List<StatusDescriptionDetailItem>(2)
                            };
                            if (!string.IsNullOrEmpty(pr.FormulaSource))
                                sdi.Details.Add(new StatusDescriptionDetailItem("Source", pr.FormulaSource));
                            if (!string.IsNullOrEmpty(pr.FormulaDescription))
                                sdi.Details.Add(new StatusDescriptionDetailItem("Description", pr.FormulaDescription));

                            if (!string.IsNullOrEmpty(pr.FormulaResult))
                                sdi.Details.Add(new StatusDescriptionDetailItem("Result", pr.FormulaResult));
                            if (!string.IsNullOrEmpty(pr.FormulaResult))
                                if (pr.Severity == ValidationRuleSeverity.Warning)
                                {
                                    sdi.Details.Add(new StatusDescriptionDetailItem("Severity", "WARNING"));
                                }
                            return sdi;
                        }), pdfFile);
                }

                //get processing end time as late as possible
                var processingEndTime = DateTime.Now;
                XmlStatusFileGeneratorService.GenerateFileAsync(request, xmlStatusFilePath, processingEndTime);
                using (var transaction = TransactionUtils.CreateAsyncTransactionScope())
                {
                    using (var connection = new SqlConnection(_settings.DbConnectionString))
                    {
                        await connection.OpenAsync(cancellationToken);
                        await connection.ExecuteAsync(new CommandDefinition(
                            (submissionStatus == SubmissionStatus.Accepted
                                ? $"UPDATE dbo.SubmittedReport SET SubmittedReportStatus = {(int) SubmittedReportStatus.Resubmitted} " +
                                  "WHERE ReportVersionId = @repVerId " +
                                  "AND UndertakingId = @undertakingId " +
                                  "AND ReportingPeriod = @period " +
                                  $"AND SubmittedReportStatus = {(int) SubmittedReportStatus.Accepted}; "
                                : ""
                            ) +
                            "INSERT INTO dbo.SubmittedReport (ReportId, ReportVersionId, ReportingPeriod, UndertakingId, SubmissionTime, " +
                            "ProcessingStartTime, ProcessingEndTime, SubmittedReportStatus, XmlLocation,  XmlProcessReportLocation, PdfProcessReportLocation, IsViewable, ReportStatusId, ExcelReportFilePath) " +
                            "VALUES (@reportId, @repVerId, @period, @undertakingId, @submissionTime, @procStart, @procEnd, @reportStatus, @xmlLocation, @xmlStatusLocation, @pdfStatusLocation, @isViewable, @reportStatusId, @excelLocation)"
                            , new
                            {
                                reportId = request.ReportId,
                                repVerId = request.ReportVersionId,
                                period = request.SubmissionInfo.ReportPeriod,
                                undertakingId = bankInfo.Id,
                                submissionTime = request.TimeSubmitted,
                                procStart = request.ProcessingStart,
                                procEnd = processingEndTime,
                                reportStatus = request.SubmissionInfo.SubmissionStatus,
                                xmlLocation = xmlPath,
                                xmlStatusLocation = xmlStatusFilePath,
                                pdfStatusLocation = pdfStatusFilePath,
                                isViewable = request.IsViewable,
                                reportStatusId = request.Id,
                                excelLocation = excelPath
                            }, cancellationToken: cancellationToken));
                    }


                    await _mediator.Send(
                        new ReportStatus.Update.Command(request.Token, StatusCodeDto.FromStatus(submissionStatus),
                            StatusCodeDto.FromStatus(dataProcessingStatus), xmlStatusFilePath, pdfStatusFilePath, null),
                        cancellationToken);

                    transaction.Complete();
                }

                var fireAndForget = _mediator.Publish(new ReportSubmittedNotification(request, xmlPath,
                    xmlStatusFilePath,
                    pdfStatusFilePath, bankInfo), cancellationToken);
                
                if (request.IsViewable)
                {
                    Task.Run(() => GenerateExcelFile(request.ReportData, request.Id, excelPath), cancellationToken)
                        .ConfigureAwait(false);
                }

                if (submissionStatus == SubmissionStatus.Accepted)
                {
                    var reportDefinitionData =
                        ReportDefinitionCacheManager.GetConfiguration(request.ReportVersionId ?? 0, _settings,
                            _cultureInfo);
                    MappingEngine.ProcessInstanceForAnalytics(request.Id, request.ReportData,
                        reportDefinitionData.FormWarehouseConfigs, _settings.DbConnectionString);
                }
            }
            catch (Exception)
            {
                try
                {
                    if (File.Exists(xmlPath))
                        File.Delete(xmlPath);
                }
                catch (Exception e)

                {
                    _logger.Warn(e, "Error during post-exception cleanup.");
                }

                try
                {
                    if (File.Exists(excelPath))
                        File.Delete(excelPath);
                }
                catch (Exception e)
                {
                    _logger.Warn(e, "Error during post-exception cleanup.");
                }

                try
                {
                    if (File.Exists(xmlStatusFilePath))
                        File.Delete(xmlStatusFilePath);
                }
                catch (Exception e)
                {
                    _logger.Warn(e, "Error during post-exception cleanup.");
                }

                try
                {
                    if (File.Exists(pdfStatusFilePath))
                        File.Delete(pdfStatusFilePath);
                }
                catch (Exception e)
                {
                    _logger.Warn(e, "Error during post-exception cleanup.");
                }

                throw;
            }
        }
    }
}