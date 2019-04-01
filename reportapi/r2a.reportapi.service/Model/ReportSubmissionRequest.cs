using System;
using System.Collections.Generic;
using System.IO;
using MediatR;
using Pinecone.ReportDataConverter;
using R2A.ReportApi.Models;
using R2A.ReportApi.Service.ReportSubmission.ReportStatus;
using Tester.Model;

namespace R2A.ReportApi.Service.Model
{
    public class ReportSubmissionRequest : IRequest
    {
        public Guid Token { get; }
        public IDictionary<string, string> Headers { get; }
        public int Id { get; }

        public DateTime ProcessingStart { get; } = DateTime.Now;
        public DateTime TimeSubmitted { get; }

        public SubmissionInfo SubmissionInfo { get; }
        public List<ValidationRule> ProcessingResult { get; }

        public string Base64EncodedFile { get; internal set;  }

        public ReportSubmissionRequest(ReportSubmissionDto model, ReportStatusInfo info, IDictionary<string, string> headers)
        {
            Token = info.Token;
            Id = info.Id;
            TimeSubmitted = info.TimeSubmitted;
            Headers = headers;
            SubmissionInfo = new SubmissionInfo
            {
                ReportCode = model.ReportInfo.ReportCode,
                ReportPeriod = model.ReportInfo.PeriodInfo,
                Undertaking = model.ReportInfo.UndertakingId
            };
            Base64EncodedFile = model.ReportFile;
                
            ProcessingResult = new List<ValidationRule>();
        }


        //TODO tehnicki nije tocno, nije garantirano da je XmlFileContents stvarno byte array od xml file-a
        public bool IsFileValid => XmlFileContents != null;   // Is file decoded and unzipped
        public bool IsModelValid { get; set; } = true;        // Has XML passed XSD validation check
        public bool IsReportValid { get; set; } = true;       // Has complete report been successfully validated
        public bool IsViewable { get; set; } = false;          // Is any of the data viewable on UI


        public byte[] XmlFileContents { get; set; }
        
        public ReportData ReportData { get; set; }

        public int? ReportId { get; set; } = null;
        public int? ReportVersionId { get; set; } = null;
        public int? UndertakingId { get; set; } = null;
        public ReportingPeriod ReportingPeriod { get; set; }
    }
}
