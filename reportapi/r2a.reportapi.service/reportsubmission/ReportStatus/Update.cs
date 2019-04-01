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

namespace R2A.ReportApi.Service.ReportSubmission.ReportStatus
{
    public static class Update
    {
        public class Command : IRequest
        {
            public Command(Guid token, StatusCodeDto submissionStatus, StatusCodeDto dataProcessingStatus, string statusFilePath, string pdfReportFilePath, string excelReportFilePath)
            {
                Token = token;
                SubmissionStatus = submissionStatus;
                DataProcessingStatus = dataProcessingStatus;
                StatusFilePath = statusFilePath;
                PdfReportFilePath = pdfReportFilePath;
                ExcelReportFilePath = excelReportFilePath;
                UpdateFilePaths = true;
            }

            public Command(Guid token, StatusCodeDto submissionStatus, StatusCodeDto dataProcessingStatus)
            {
                Token = token;
                SubmissionStatus = submissionStatus;
                DataProcessingStatus = dataProcessingStatus;
                UpdateFilePaths = false;
            }

            public Guid Token { get; }
            public StatusCodeDto SubmissionStatus { get; }

            public StatusCodeDto DataProcessingStatus { get; }

            public string StatusFilePath { get; }
            public string PdfReportFilePath { get; }
            public string ExcelReportFilePath { get; set; }
            public bool UpdateFilePaths { get; }
        }

        public class Handler : IRequestHandler<Command>
        {
            private readonly Settings _settings;
            public Handler(Settings settings)
            {
                _settings = settings;
            }
            public async Task Handle(Command request, CancellationToken cancellationToken)
            {
                var settersList = new List<string>(3);
                if (request.SubmissionStatus != null)
                {
                    settersList.Add("SubmissionStatus = @subStatus, SubmissionStatusMessage = @subMessage");
                }

                if (request.DataProcessingStatus != null)
                {
                    settersList.Add("DataProcessingStatus = @datStatus, DataProcessingStatusMessage = @datMessage");
                }

                if (request.UpdateFilePaths)
                {
                    settersList.Add("StatusFilePath = @xmlPath, PdfReportFilePath = @pdfPath");
                    if (!string.IsNullOrEmpty(request.ExcelReportFilePath))
                    {
                        settersList.Add("ExcelReportFilePath = @excelPath");
                    }
                }

                if (settersList.Count == 0)
                    return;

                var setters = settersList.Aggregate((s1, s2) => $"{s1}, {s2}");
                
                using (var connection = new SqlConnection(_settings.DbConnectionString))
                {
                    await connection.OpenAsync(cancellationToken);
                    await connection.ExecuteAsync(new CommandDefinition(
                        "UPDATE dbo.ReportStatus SET " +
                        setters +
                        " WHERE Token = @token", new
                        {
                            token = request.Token,
                            subStatus = request.SubmissionStatus?.StatusCode,
                            subMessage = request.SubmissionStatus?.StatusMessage,
                            datStatus = request.DataProcessingStatus?.StatusCode,
                            datMessage = request.DataProcessingStatus?.StatusMessage,
                            xmlPath = request.StatusFilePath,
                            pdfPath = request.PdfReportFilePath,
                            excelPath = request.ExcelReportFilePath
                        }, cancellationToken: cancellationToken));
                }
            }
        }
    }
}
