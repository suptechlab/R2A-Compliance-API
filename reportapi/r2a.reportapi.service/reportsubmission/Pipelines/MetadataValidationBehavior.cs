using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using R2A.ReportApi.Models;
using R2A.ReportApi.Service.Infrastructure;
using R2A.ReportApi.Service.Model;
using Tester.Model;

namespace R2A.ReportApi.Service.ReportSubmission.Pipelines
{
    public class MetadataValidationBehavior : IPipelineBehavior<ReportSubmissionRequest, Unit>
    {
        //private readonly Logger _logger;
        private readonly Settings _settings;
        private const int BankCodeNumOfDigits = 6;

        public MetadataValidationBehavior(Settings settings)
        {
            _settings = settings;
            //_logger = logFactory.GetCurrentClassLogger();
        }


        public async Task<Unit> Handle(ReportSubmissionRequest request, CancellationToken cancellationToken,
            RequestHandlerDelegate<Unit> next)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var submissionInfo = request.SubmissionInfo;
            var metadataProcessingResult = new List<ValidationRule>();

            var undertakingCode =
                ExtractUndertakingCode(request.Headers[MessageHeaders.Subject], _settings.CertificateNamePrefix);
            request.UndertakingId = await ValidateUndertakingAsync(submissionInfo.Undertaking, undertakingCode,
                metadataProcessingResult, cancellationToken);
            

            var reportWithRecurrenceType = await ValidateReportCodeAsync(submissionInfo.ReportCode,
                metadataProcessingResult, cancellationToken);

            if (reportWithRecurrenceType != null)
            {
                var reportId = reportWithRecurrenceType.Item1;
                var periodEndtime = ValidatePeriod(submissionInfo.ReportPeriod, reportWithRecurrenceType.Item2,
                    metadataProcessingResult, out var reportingPeriod);
                if (periodEndtime != null)
                {
                    request.ReportingPeriod = reportingPeriod;
                    request.ReportId = reportId;
                    request.ReportVersionId = await LoadReport(reportId, submissionInfo.ReportCode, periodEndtime.Value,
                        submissionInfo.ReportPeriod, metadataProcessingResult, cancellationToken);                    
                }
            }


            if (metadataProcessingResult.Any())
            {
                request.IsModelValid = false;
                request.ProcessingResult.AddRange(metadataProcessingResult);
            }
            else
            {
                request.IsModelValid = true;
            }

            return await next();
        }

        private DateTime? ValidatePeriod(string reportPeriod, string recurrenceType,
            List<ValidationRule> requestProcessingResult, out ReportingPeriod reportingPeriod)
        {
            try
            {
                reportingPeriod = new ReportingPeriod(reportPeriod, recurrenceType[0]);
                if (reportingPeriod.Type != null)
                {
                    return reportingPeriod.EndingDate;
                }
            }
            catch (Exception)
            {
                //valid false
            }
            requestProcessingResult.Add(ValidationRule.Error(ValidationRuleConstant.ReportPeriodFormatInvalidErrorCode,
                $"Report period ({reportPeriod}) format invalid for recurrence type {ReportingPeriodTypeExtensions.From(recurrenceType[0])}."));
            reportingPeriod = null;
            return null;
        }

        private async Task<Tuple<int, string>> ValidateReportCodeAsync(string reportCode,
            List<ValidationRule> requestProcessingResult,
            CancellationToken cancellationToken)
        {
            using (var connection = new SqlConnection(_settings.DbConnectionString))
            {
                await connection.OpenAsync(cancellationToken);
                var sql = @"SELECT
                              r.Id,
                              r.RecurrenceType
                            FROM Report AS r
                            WHERE
                              r.Code =  @reportCode;
                          ";
                var result = await connection.QueryFirstOrDefaultAsync(new CommandDefinition(sql,
                    new {reportCode}, cancellationToken: cancellationToken));
                if (result == null)
                {
                    requestProcessingResult.Add(ValidationRule.Error(ValidationRuleConstant.ReportNotFoundErrorCode,
                        $"Report not found for report code {reportCode}."));
                    return null;
                }

                return Tuple.Create(result.Id, result.RecurrenceType);
            }
        }

        private async Task<int?> LoadReport(int reportId, string reportCode, DateTime periodEndtime,
            string period,
            List<ValidationRule> requestProcessingResult,
            CancellationToken cancellationToken)
        {
            using (var connection = new SqlConnection(_settings.DbConnectionString))
            {
                await connection.OpenAsync(cancellationToken);
                var sql = @"SELECT r.Id AS ReportId, r.Code,rv.Id as ReportVersionId
                        FROM Report AS r
                          JOIN ReportVersion AS rv ON rv.ReportId = r.Id
                        WHERE
                          r.Id = @reportId
                          AND rv.ReportingPeriodFrom <= @periodEndtime
                          AND (rv.ReportingPeriodTo > @periodEndtime OR rv.ReportingPeriodTo IS NULL)
                          AND rv.Active = 1;
                        ";

                var result = await connection.QueryFirstOrDefaultAsync(new CommandDefinition(sql,
                    new {reportId, periodEndtime}, cancellationToken: cancellationToken));
                if (result == null)
                {
                    requestProcessingResult.Add(ValidationRule.Error(ValidationRuleConstant.ReportVersionNotFoundErrorCode,
                        $"Active report version not found for report {reportCode} for period {period}."));
                    return null;
                }

                return result.ReportVersionId;
            }
        }

        private static string ExtractUndertakingCode(string commonName, string certificateNamePrefix)
        {
            if (!commonName.Contains(certificateNamePrefix))
                return null;
            int match = commonName.IndexOf(certificateNamePrefix, StringComparison.Ordinal);
            if (match + certificateNamePrefix.Length + BankCodeNumOfDigits > commonName.Length)
                return null;
            return commonName.Substring(match + certificateNamePrefix.Length,
                BankCodeNumOfDigits);
        }


        private async Task<int?> ValidateUndertakingAsync(string undertaking, string headerUndertaking,
            List<ValidationRule> requestProcessingResult,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(undertaking))
            {
                requestProcessingResult.Add(ValidationRule.Error(ValidationRuleConstant.BankNotSpecifiedErrorCode,
                    "Bank not specified in submition request."));
                return null;
            }

            if (string.IsNullOrEmpty(headerUndertaking))
            {
                requestProcessingResult.Add(ValidationRule.Error(ValidationRuleConstant.BankNotSpecifiedErrorCode,
                    "Certificate subject name does not contain bank code."));
                return null;
            }

            if (undertaking != headerUndertaking)
            {
                requestProcessingResult.Add(ValidationRule.Error(ValidationRuleConstant.BankNotAllowedErrorCode,
                    $"Bank code {undertaking} in request not allowed for certificate code {headerUndertaking} ."));
                return null;
            }

            using (var connection = new SqlConnection(_settings.DbConnectionString))
            {
                await connection.OpenAsync(cancellationToken);
                var undertakingId = await connection.QueryFirstOrDefaultAsync<int>(new CommandDefinition(
                    @"SELECT Id FROM bsp.Bank
                WHERE Code = @undertaking", new {undertaking}, cancellationToken: cancellationToken));
                if (undertakingId == 0)
                {
                    requestProcessingResult.Add(ValidationRule.Error(ValidationRuleConstant.BankNotFoundErrorCode,
                        $"Bank code {undertaking} not registerd."));
                    return null;
                }
                return undertakingId;
            }
        }
    }
}