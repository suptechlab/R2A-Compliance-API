using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using R2A.ReportApi.Models;
using R2A.ReportApi.Service.Infrastructure;

namespace R2A.ReportApi.Service.ReportSubmission.ReportStatus
{
    public class ReportStatusInfo
    {
        public Guid Token { get; set; }
        public int Id { get; set; }
        public StatusCodeDto SubmissionStatus { get; set; }
        public StatusCodeDto DataProcessingStatus { get; set; }
        public DateTime TimeSubmitted { get; set; }
    }
    public static class GetInfo
    {
        public class Query : IRequest<Response>
        {
            public Guid Token { get; }
            public Query(Guid token)
            {
                Token = token;
            }
        }

        public class Response
        {
            public ReportStatusInfo Result { get; set; }
        }

        public class Handler : IRequestHandler<Query,Response>
        {
            private readonly Settings _settings;
            public Handler(Settings settings)
            {
                _settings = settings;
            }

            public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
            {
                using (var connection = new SqlConnection(_settings.DbConnectionString))
                {
                    await connection.OpenAsync(cancellationToken);
                    var dbResult = await connection.QueryFirstOrDefaultAsync(new CommandDefinition(
                        "SELECT Id, SubmissionStatus, SubmissionStatusMessage, DataProcessingStatus, DataProcessingStatusMessage, TimeSubmitted " +
                        "FROM dbo.ReportStatus " +
                        "WHERE Token = @token", new {token = request.Token}, cancellationToken: cancellationToken));
                    return new Response()
                    {
                        Result = new ReportStatusInfo()
                        {
                            Token = request.Token,
                            Id = dbResult.Id,
                            SubmissionStatus = new StatusCodeDto()
                            {
                                StatusCode = dbResult.SubmissionStatus,
                                StatusMessage = dbResult.SubmissionStatusMessage,
                            },
                            DataProcessingStatus = new StatusCodeDto()
                            {
                                StatusCode = dbResult.DataProcessingStatus,
                                StatusMessage = dbResult.DataProcessingStatusMessage
                            },
                            TimeSubmitted = dbResult.TimeSubmitted
                        }
                    };
                }
            }
        }
    }
}
