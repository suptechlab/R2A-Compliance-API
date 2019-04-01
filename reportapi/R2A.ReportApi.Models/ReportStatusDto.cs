namespace R2A.ReportApi.Models
{
    public class ReportStatusDto
    {
        public StatusCodeDto SubmissionStatus { get; set; }

        public StatusCodeDto DataProcessingStatus { get; set; }

        public bool StatusFileAvailable { get; set; }
        public bool ExcelFileAvailable { get; set; }
    }

}
