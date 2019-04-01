using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using R2A.ReportApi.Models;
using R2A.ReportApi.Service.Infrastructure;
using R2A.ReportApi.Service.ReportSubmission.ReportStatus;

namespace R2A.ReportApi.Service.Model
{
    public class BankInfo
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public string EmailAddress { get; set; }
    }

    public static class GetBankInfoFromCode
    {
        public class Query : IRequest<Result>
        {
            public Query(string code)
            {
                Code = code;
            }

            public string Code { get; }
        }

        public class Result
        {
            public BankInfo BankInfo { get; set; }
        }

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly string _connectionString;
            public Handler(Settings settings)
            {
                _connectionString = settings.DbConnectionString;
            }

            public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync(cancellationToken);
                    return new Result()
                    {
                        BankInfo = await connection.QueryFirstOrDefaultAsync<BankInfo>(new CommandDefinition(
                            "SELECT Id, Code, Title, EmailAddress " +
                            "FROM bsp.Bank " +
                            "WHERE Code = @code", new {code = request.Code}, cancellationToken: cancellationToken))

                    };
                }
            }
        }
    }
}
