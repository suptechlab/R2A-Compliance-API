using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using R2A.ReportApi.Client.Common;


namespace R2A.ReportApi.Client.ReportSubmission
{
    public static class GetStatusFilePath
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
            public string FilePath { get; set; }
            public string FilePathPdf { get; set; }
            public string FilePathExcel { get; set; }
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
                        "    rs.StatusFilePath AS FilePath, " +
                        "    rs.PdfReportFilePath AS FilePathPdf, " +
                        "    rs.ExcelReportFilePath AS FilePathExcel " +
                        "FROM dbo.ReportStatus AS rs " +
                        "WHERE rs.Token = @token AND rs.BankCode = @bankCode ";


                    var result = await connection.QueryFirstOrDefaultAsync<Result>(sql, new { token = request.Token, bankCode = request.BankCode });
                    return result;
                }
            }
        }
    }
}
