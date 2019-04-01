using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using R2A.ReportApi.Service.Infrastructure;
using R2A.ReportApi.Service.Model;

namespace R2A.ReportApi.Service.ReportSubmission.Pipelines
{
    
    
    public class BankValidationBehavior : IPipelineBehavior<ReportSubmissionRequest, Unit>
    {
        private BSPSettings _settings;

        public BankValidationBehavior(BSPSettings settings)
        {
            this._settings = settings;
        }

        public async Task<Unit> Handle(ReportSubmissionRequest request, CancellationToken cancellationToken,
            RequestHandlerDelegate<Unit> next)
        {

            var processResultList = new List<ValidationRule>();
            if (!request.UndertakingId.HasValue)
            {
                request.IsModelValid = false;
            }
            else
            {
                
                using (var connection = new SqlConnection(_settings.DbConnectionString))
                {
                    await connection.OpenAsync(cancellationToken);
                    var sql = @"SELECT
                              r.IsActive                              
                            FROM Bank AS b
                            WHERE
                              b.Code =  @bankCode;
                          ";
                    var result = await connection.QueryFirstOrDefaultAsync(new CommandDefinition(sql,
                        new { bankCode = request.UndertakingId }, cancellationToken: cancellationToken));
                    if (result == null)
                    {
                        processResultList.Add(ValidationRule.Error(ValidationRuleConstant.BankNotActiveErrorCode,
                            $"Bank {request.UndertakingId} not active."));                                                
                    }
                } 
            }

            if (processResultList.Any())
            {
                request.IsModelValid = false;
                request.ProcessingResult.AddRange(processResultList);
            }
            
            return await next();
        }
    }
}