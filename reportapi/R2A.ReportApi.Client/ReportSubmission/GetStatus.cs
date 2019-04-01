using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using Newtonsoft.Json;
using R2A.ReportApi.Client.Common;
using R2A.ReportApi.Models;

namespace R2A.ReportApi.Client.ReportSubmission
{
    public static class GetStatus
    {
        public class Query : IRequest<Result>
        {
            public Guid Token { get; }
            public string BankCode { get; }

            public Query(Guid token, string bankCode)
            {
                Token = token;
                BankCode = bankCode;
            }
        }

        public class Result
        {
            public ReportStatusDto Status { get; set; }
        }

        public class Handler : IRequestHandler<Query,Result>
        {
            private readonly Settings _settings;

            public Handler(Settings settings)
            {
                _settings = settings;
            }

            public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
            {
                using (var connection = new SqlConnection(_settings.DbConnectionString))
                {
                    await connection.OpenAsync(cancellationToken);

                    var sql =
                        "SELECT " +
                        "    rs.SubmissionStatus, " +
                        "    rs.SubmissionStatusMessage, " +
                        "    rs.DataProcessingStatus, " +
                        "    rs.DataProcessingStatusMessage, " +
                        "    CASE WHEN rs.StatusFilePath IS NULL THEN 0 ELSE 1 END AS StatusFileAvailable, " +
                        "    CASE WHEN rs.ExcelReportFilePath IS NULL THEN 0 ELSE 1 END AS ExcelFileAvailable " +
                        "FROM dbo.ReportStatus AS rs " +
                        "WHERE rs.Token = @token AND rs.BankCode = @bankCode ";


                    var dbResult = await connection.QueryFirstOrDefaultAsync(sql, new {token = request.Token, bankCode = request.BankCode });
                    if(dbResult == null)
                        return new Result();

                    return new Result()
                    {
                        Status = new ReportStatusDto()
                        {
                            SubmissionStatus = new StatusCodeDto()
                            {
                                StatusCode = dbResult.SubmissionStatus,
                                StatusMessage = dbResult.SubmissionStatusMessage
                            },
                            DataProcessingStatus = new StatusCodeDto()
                            {
                                StatusCode = dbResult.DataProcessingStatus,
                                StatusMessage = dbResult.DataProcessingStatusMessage
                            },
                            StatusFileAvailable = dbResult.StatusFileAvailable == 1,
                            ExcelFileAvailable = dbResult.ExcelFileAvailable == 1
                        }
                    };
                }
            }
        }
    }
}
