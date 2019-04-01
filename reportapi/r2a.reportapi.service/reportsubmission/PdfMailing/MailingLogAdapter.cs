using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using R2A.ReportApi.Service.Infrastructure;
using SendMailUtilities.Utilities;

namespace R2A.ReportApi.Service.ReportSubmission.PdfMailing
{
    public class MailingLogAdapter : SendMailUtilities.Utilities.ILogger
    {
        private readonly NLog.ILogger _logger;

        public MailingLogAdapter(ILogFactory logFactory)
        {
            _logger = logFactory.GetLogger<EmailSender>();
        }

        public void Error(Exception e, string message)
        {
            _logger.Error(e,message);
        }
    }
}
