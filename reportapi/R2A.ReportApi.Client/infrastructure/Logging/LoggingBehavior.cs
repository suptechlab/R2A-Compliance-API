using System.Diagnostics;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using R2A.ReportApi.Client.Authentication;
using ServiceStack.Text;

namespace R2A.ReportApi.Client.Infrastructure.Logging
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IHttpContextAccessor _context;

        private readonly ILogger _logger;
        

        public LoggingBehavior(ILoggerFactory loggerFactory, IHttpContextAccessor context)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger("LoggingBehavior");
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken,
            RequestHandlerDelegate<TResponse> next)
        {
#if DEBUG
            var timer = Stopwatch.StartNew();
            try
            {
#endif
                var bankCode = _context.HttpContext.User.Claims.GetValueOfType(ClaimTypes.Sid);
                _logger.LogInformation("[{bankCode}] Processing request {requestName}: {request}",
                    bankCode,  typeof(TRequest).FullName, request.Dump());
                
                var result = await next();

                _logger.LogInformation(
                    "[{bankCode}] Returning from request {requestName} with response: {response}",
                    bankCode, typeof(TRequest).FullName, result.Dump());
                return result;
#if DEBUG
            }
            finally
            {
                timer.Stop();
                _logger.LogDebug(
                    $"Executed handler of {typeof(TRequest).FullName} request in {timer.ElapsedMilliseconds}ms.");
            }
#endif
        }
    }
}