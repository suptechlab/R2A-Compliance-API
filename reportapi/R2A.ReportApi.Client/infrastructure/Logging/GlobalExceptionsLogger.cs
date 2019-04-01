using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;


namespace R2A.ReportApi.Client.Infrastructure.Logging
{
    public class GlobalExceptionsLogger
    {
        private readonly ILogger _logger;

        public GlobalExceptionsLogger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GlobalExceptionsLogger>();
        }


        public async Task Invoke(HttpContext context, Func<Task> next)
        {
            // Do something before
            try
            {
                await next();
            }
            // Do something after
            catch (Exception e)
            {
                _logger.LogError(e,"Uncaught exception");
                throw;
            }
        }
    }
}