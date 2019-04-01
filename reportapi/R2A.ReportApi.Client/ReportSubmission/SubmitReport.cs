using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using MediatR;
using Newtonsoft.Json;
using R2A.ReportApi.Client.Authentication.Certificate;
using R2A.ReportApi.Client.Common;
using R2A.ReportApi.Client.Infrastructure;
using R2A.ReportApi.Models;

namespace R2A.ReportApi.Client.ReportSubmission
{
    public static class SubmitReport
    {
        public class Command : IRequest<Result>
        {
            public ReportSubmissionDto Model { get; }
            public UserCertificateInfo UserInfo { get; }

            public Command(ReportSubmissionDto model, UserCertificateInfo userInfo)
            {
                Model = model;
                UserInfo = userInfo;
            }
        }

        public class Result
        {
            public Guid Token { get; set; }

        }

        public class Handler : IRequestHandler<Command,Result>
        {
            private readonly Settings _settings;
            private readonly MessageQueueService _mqService;

            public Handler(Settings settings, MessageQueueService mqService)
            {
                _settings = settings;
                _mqService = mqService;
            }

            public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
            {
                Guid token = Guid.NewGuid();
                
                using (var connection = new SqlConnection(_settings.DbConnectionString))
                {
                    await connection.OpenAsync(cancellationToken);
                    
                    
                    var sql =
                        "INSERT INTO dbo.ReportStatus(Token,ReportCode,BankCode,PeriodInfo,SubmissionStatus,SubmissionStatusMessage) " +
                        "VALUES (@token,@ReportCode,@BankCode,@PeriodInfo,@submissionStatus,@submissionStatusMessage)";

                    await connection.ExecuteAsync(new CommandDefinition(sql, new
                    {
                        token, submissionStatus = SubmissionStatus.InQueue.Code, submissionStatusMessage = SubmissionStatus.InQueue.ToString(),
                        BankCode = request.Model.ReportInfo.UndertakingId, request.Model.ReportInfo.ReportCode, request.Model.ReportInfo.PeriodInfo
                    }, cancellationToken: cancellationToken));
                }

                var headers = request.UserInfo.ToSparseDictionary();
                headers[MessageHeaders.Token] = token.ToString();
                _mqService.SendMessage(headers,JsonConvert.SerializeObject(request.Model));

                return new Result() { Token = token };
            }
        }
    }
}
