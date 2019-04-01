using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using R2A.ReportApi.Service.Model;

namespace R2A.ReportApi.Service.ReportSubmission
{
    public class ReportSubmittedNotification : INotification
    {
        public ReportSubmittedNotification(ReportSubmissionRequest request, string xmlPath, string xmlStatusFilePath, string pdfStatusFilePath, BankInfo bankInfo)
        {
            Request = request;
            XmlPath = xmlPath;
            XmlStatusFilePath = xmlStatusFilePath;
            PdfStatusFilePath = pdfStatusFilePath;
            BankInfo = bankInfo;
        }

        public ReportSubmissionRequest Request { get; }

        public BankInfo BankInfo { get; }

        public string XmlPath { get; }

        public string XmlStatusFilePath { get; }

        public string PdfStatusFilePath { get; }
    }
}