using System;

namespace R2A.ReportApi.Models
{
    public class StatusCodeDto
    {
        public string StatusCode { get; set; }
        public string StatusMessage { get; set; }

        public static StatusCodeDto FromStatus(StatusCode status, string message = null)
        {
            return new StatusCodeDto()
            {
                StatusCode = status.Code,
                StatusMessage = $"{status.Label}{(String.IsNullOrEmpty(message)?"":$": {message}")}"
            };
        }
    }
}
