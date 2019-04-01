using System;

namespace Tester.Model
{
    public class SubmissionInfo
    {
        public string ReportCode { get; set; }
        public string ReportPeriod { get; set; }
        public string Undertaking { get; set; }
        public SubmittedReportStatus SubmissionStatus { get; set; }
    }
}