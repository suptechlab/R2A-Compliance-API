using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace R2A.ReportApi.Client.Infrastructure.Errors
{
    /// <summary>
    /// Hvataj ErrorResponseException greske i napravi odgovarajuci JSON response (ako je API poziv).
    /// </summary>
    public class ErrorResponseMiddleware
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private readonly ILogger _logger;

        public ErrorResponseMiddleware(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(GetType());
        }

        public async Task Invoke(HttpContext context, Func<Task> next)
        {
            try
            {
                await next();
            }
            catch (ErrorResponseException e)
            {
                await HandleExceptionAsync(context, e);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, ErrorResponseException responseException)
        {
            _logger.LogError(responseException, "ErrorResponseMiddleware caught an exception.");
            context.Response.StatusCode = responseException.ResponseStatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonConvert.SerializeObject(responseException.Errors, JsonSettings));
        }
    }
}