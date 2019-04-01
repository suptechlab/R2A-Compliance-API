using System.Collections.Generic;
using System.Net;
using System.Text;

namespace R2A.ReportApi.Client.Common.Errors
{
    public class ErrorResponse
    {
        public string Code { get; set; }
        public string Field { get; set; }
        public BaseErrorMeta Meta { get; set; }

        public ErrorResponse()
        {
            Code = ErrorResponseCode.Unknown;
        }

        public static IEnumerable<ErrorResponse> Single(ErrorResponse response)
        {
            return new ErrorResponse[] {response};
        }

        public static ErrorResponse NotFound(string customMessage = null)
        {
            if (string.IsNullOrEmpty(customMessage))
            {
                customMessage = "The requested resource was not found";
            }
            return new ErrorResponse()
            {
                Code = ErrorResponseCode.FromHttpStatus(HttpStatusCode.NotFound),
                Meta = new BaseErrorMeta() { Message = customMessage }
            };
        }

        public static ErrorResponse HttpError(int httpCode, string message)
        {
            return new ErrorResponse()
            {
                Code = ErrorResponseCode.FromHttpStatus(httpCode),
                Meta = new BaseErrorMeta() { Message = message}
            };
        }


        public override string ToString()
        {
            StringBuilder bld = new StringBuilder();
            bld.AppendFormat("[{0}]", Code);
            if (!string.IsNullOrEmpty(Field) || Meta != null)
                bld.Append(": ");
            if (!string.IsNullOrEmpty(Field))
                bld.AppendFormat("Field: {0} ", Field);
            if (Meta != null)
            {
                bld.AppendFormat("Meta: {0}", Meta.Message);
            }

            return bld.ToString();
        }
    }

    public static class ErrorResponseCode
    {
        public const string Authorization = "E1000";
        public const string ValidationReportFile = "E1001";
        public const string ValidationReportInfo = "E1002";
        public const string InvalidToken = "E1003";
        public const string ValidationReport = "E1004";
        public const string Unknown = "E9000";
        public const string DatabaseError = "E1005";
        public const string ValidationRegistration = "E1006";

        public static string FromHttpStatus(HttpStatusCode status)
        {
            return FromHttpStatus((int) status);
        }

        public static string FromHttpStatus(int status)
        {
            if (status == 401 || status == 403)
                return Authorization;
            return $"E0{status}";
        }
    }
}
