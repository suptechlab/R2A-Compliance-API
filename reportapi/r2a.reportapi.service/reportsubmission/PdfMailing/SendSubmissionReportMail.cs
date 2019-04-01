using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using R2A.ReportApi.Models;
using R2A.ReportApi.Service.Infrastructure;
using SendMailUtilities.Utilities;
using Tester.Model;
using NLog;
using R2A.ReportApi.Service.Model;
using ILogger = NLog.ILogger;

namespace R2A.ReportApi.Service.ReportSubmission.PdfMailing
{
    public class SendSubmissionReportMail : INotificationHandler<ReportSubmittedNotification>
    {
        private readonly Settings _settings;
        private readonly EmailSender _mailingService;
        private readonly ILogger _logger;

        public SendSubmissionReportMail(Settings settings, EmailSender mailingService, ILogFactory logFactory)
        {
            _settings = settings;
            _mailingService = mailingService;
            _logger = logFactory.GetLogger<SendSubmissionReportMail>();
        }

        public async Task Handle(ReportSubmittedNotification notification, CancellationToken cancellationToken)
        {
            if (!_settings.ShouldSendPdfMail)
                return;
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var mailSpecs = new PdfStatusReportMail(notification.BankInfo.EmailAddress,
                    _mailingService.Configuration.FromAddress, notification.Request.SubmissionInfo.ReportCode,
                    notification.BankInfo.Title, notification.Request.ReportingPeriod.EndingDate,
                    notification.Request.TimeSubmitted,
                    notification.Request.SubmissionInfo.SubmissionStatus, notification.PdfStatusFilePath);
                if (_settings.PdfMailAdditionalAddresses != null && _settings.PdfMailAdditionalAddresses.Any())
                {
                    mailSpecs.Recipitents.AddRange(_settings.PdfMailAdditionalAddresses);
                }

                var sent = await _mailingService.SendEmailAsync(mailSpecs, cancellationToken);
                if (!sent)
                {
                    _logger.Warn($"Unable to send submission confirmation mail for ReportStatus ID {notification.Request.Id}.");
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Unable to send submission confirmation mail for ReportStatus ID {notification.Request.Id}.");
            }
        }
    }

    public class PdfStatusReportMail : IMailSpecification
    {
        public List<string> Recipitents { get; } = new List<string>();
        public string Subject { get; }
        public string From { get; }
        public string PlainTextMessage { get; }
        public string HtmlMessage { get; } = null;
        public List<MailAttachment> Attachments { get; } = new List<MailAttachment>(1);
        public string ReplyTo { get; } = null;

        public PdfStatusReportMail(string recepientAddress, string senderAddress, string reportCode, string undertakingName, DateTime periodEndDate, DateTime submissionTime, SubmittedReportStatus status, string pdfLocation)
        {
            if (!string.IsNullOrEmpty(recepientAddress))
            {
                Recipitents.Add(recepientAddress);
            }

            From = senderAddress;
            if (File.Exists(pdfLocation))
            {
                Attachments.Add(new MailAttachment()
                {
                    Contents = File.ReadAllBytes(pdfLocation),
                    Name = Path.GetFileName(pdfLocation)
                });
            }

            Subject = $"[{undertakingName}] Report submission confirmation for {reportCode} {periodEndDate: dd MMM yyyy}";

            PlainTextMessage =
                $"This is a confirmation that {undertakingName} submitted on {submissionTime:dd MMM yyyy} at {submissionTime:h:mm:ss tt} " +
                $"for the reporting period {periodEndDate: dd MMM yyyy} the prescribed {reportCode} along with the other related/relevant reports. " +
                $"Said submission was processed and {(status == SubmittedReportStatus.Accepted?"found to have passed system validation with no noted findings":"were noted to have findings.")}";
            if (Attachments.Any())
            {
                PlainTextMessage += status == SubmittedReportStatus.Accepted
                    ? " as seen in the attached PDF."
                    : " Details of such are enumerated in the attached PDF.";
            }
            else if (status == SubmittedReportStatus.Accepted)
            {
                PlainTextMessage += ".";
            }

        }
    }


}
