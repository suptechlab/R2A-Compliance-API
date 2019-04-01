using System.Threading;
using System.Threading.Tasks;
using IDD.Infrastructure.Transactions;
using MediatR;

namespace R2A.ReportApi.Client.Infrastructure
{
    public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>

    {
        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken,
            RequestHandlerDelegate<TResponse> next)
        {
            using (var asyncTransactionScope = TransactionUtils.CreateAsyncTransactionScope())
            {
                var response = await next();
                cancellationToken.ThrowIfCancellationRequested();
                asyncTransactionScope.Complete();
                return response;
            }
        }
    }
}