using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace R2A.ReportApi.Service.ReportSubmission
{
    public static class FileNamingConvention
    {
        public static string GenerateFileNameBase(string reportCode, string bankCode, string reportPeriod, DateTime submissionTime,
            int reportStatusId)
        {
            return
                $"{reportCode}_{bankCode}_{reportPeriod.Replace('-', '_')}_{submissionTime:yyyyMMddThhmmss}_{reportStatusId}";
        }
    }
}
