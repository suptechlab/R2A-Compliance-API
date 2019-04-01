using System.Collections.Generic;

namespace R2A.ReportApi.Service.Infrastructure
{
    public class Settings
    {
        public string DbConnectionString { get; set; }

        public string ReportSubmissionQueueName { get; set; }
        public string QueueExchange { get; set; }
        public string ReportSubmissionRoutingId { get; set; }
        public string XmlFileSaveLocation { get; set; }
        public string ReportFileSaveLocation { get; set; }
        public string LogoImagePath { get; set; }
        public string MessageDumpFolder { get; set; }
        public bool UseBinaryJsonCache { get; set; }
        public string CertificateNamePrefix { get; set; }
        public bool ShouldSendPdfMail { get; set; }
        public IEnumerable<string> PdfMailAdditionalAddresses { get; set; }
    }
}