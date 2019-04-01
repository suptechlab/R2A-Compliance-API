using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using R2A.ReportApi.Models;
using R2A.ReportApi.Service.Infrastructure;
using R2A.ReportApi.Service.Model;
using R2A.ReportApi.Service.ReportSubmission;
using R2A.ReportApi.Service.ReportSubmission.ReportStatus;
using R2A.ReportApi.Service.ReportSubmission.Pipelines;
using Tester.Model;
using Xunit;

namespace R2A.ReportApi.Test461
{
    public class MetatadaValidationTests
    {
        private void TestSetup()
        {
            _request = new ReportSubmissionRequest(new ReportSubmissionDto()
            {
                ReportInfo = new ReportSubmissionInfo()
                {
                    PeriodInfo = "2017-12",
                    ReportCode = "FRP",
                    UndertakingId = "1001.a"
                }
            }, new ReportStatusInfo() 
            {

            }, new Dictionary<string, string>()
            {
                { MessageHeaders.Subject , "CodePrefix-1001.a" }
            })
            {
                XmlFileContents = new byte[] {0}
            };
            _validator = new MetadataValidationBehavior(new Settings()
            {
                DbConnectionString = "Data Source=10.100.93.6;initial catalog=R2A_S1A;persist security info=True;user id=r2a;password=r2a;MultipleActiveResultSets=True;",
                CertificateNamePrefix = "CodePrefix-"
            });
        }

        private ReportSubmissionRequest _request;
        private MetadataValidationBehavior _validator;

        [Fact]
        public void NoValidationPasses()
        {
            TestSetup();
            AssertValidationPassed();
        }

        [Fact]
        public void GoodMetadataPasses()
        {
            TestSetup();
            _validator.Handle(_request, default(CancellationToken), () => Unit.Task).Wait();
            AssertValidationPassed();
        }

        [Fact]
        public void WrongReportCodeFails()
        {
            TestSetup();
            _request.SubmissionInfo.ReportCode = "FRPv4";
            _validator.Handle(_request, default(CancellationToken), () => Unit.Task).Wait();
            AssertValidationFailed();
            AssertContainsError(ValidationRuleConstant.ReportNotFoundErrorCode);
        }

        [Fact]
        public void NotMatchingBankCodeFails()
        {
            TestSetup();
            _request.SubmissionInfo.Undertaking = "xFAKEx";
            _validator.Handle(_request, default(CancellationToken), () => Unit.Task).Wait();
            AssertValidationFailed();
            AssertContainsError(ValidationRuleConstant.BankNotAllowedErrorCode);
        }


        [Fact]
        public void InvalidBankCodeFails()
        {
            TestSetup();
            _request.SubmissionInfo.Undertaking = "xFAKEx";
            _request.Headers[MessageHeaders.Subject] = "CodePrefix-xFAKEx";
            _validator.Handle(_request, default(CancellationToken), () => Unit.Task).Wait();
            AssertValidationFailed();
            AssertContainsError(ValidationRuleConstant.BankNotFoundErrorCode);
        }

        [Fact]
        public void MissingBankCodeFails()
        {
            TestSetup();
            _request.SubmissionInfo.Undertaking = null;
            _validator.Handle(_request, default(CancellationToken), () => Unit.Task).Wait();
            AssertValidationFailed();
            AssertContainsError(ValidationRuleConstant.BankNotSpecifiedErrorCode);
        }

        [Fact]
        public void MissingReportCodeFails()
        {
            TestSetup();
            _request.SubmissionInfo.ReportCode = null;
            _validator.Handle(_request, default(CancellationToken), () => Unit.Task).Wait();
            AssertValidationFailed();
            AssertContainsError(ValidationRuleConstant.ReportNotFoundErrorCode);
        }
        [Fact]
        public void MissingPeriodInfoFails()
        {
            TestSetup();
            _request.SubmissionInfo.ReportPeriod = null;
            _validator.Handle(_request, default(CancellationToken), () => Unit.Task).Wait();
            AssertValidationFailed();
            AssertContainsError(ValidationRuleConstant.ReportPeriodFormatInvalidErrorCode);
        }

        [Fact]
        public void BadCertificateSubjectFails()
        {
            TestSetup();
            _request.Headers[MessageHeaders.Subject] = "whatever";
            _validator.Handle(_request, default(CancellationToken), () => Unit.Task).Wait();
            AssertValidationFailed();
            AssertContainsError(ValidationRuleConstant.BankNotSpecifiedErrorCode);
        }

        [Fact]
        public void PeriodInfoWrongFormatFails()
        {
            TestSetup();
            _request.SubmissionInfo.ReportPeriod = "2018";
            _validator.Handle(_request, default(CancellationToken), () => Unit.Task).Wait();
            AssertValidationFailed();
            AssertContainsError(ValidationRuleConstant.ReportPeriodFormatInvalidErrorCode);
            TestSetup();
            _request.SubmissionInfo.ReportPeriod = "whatever";
            _validator.Handle(_request, default(CancellationToken), () => Unit.Task).Wait();
            AssertValidationFailed();
            AssertContainsError(ValidationRuleConstant.ReportPeriodFormatInvalidErrorCode);
        }

        [Fact]
        public void PeriodInfoNoMatchFails()
        {
            TestSetup();
            _request.SubmissionInfo.ReportPeriod = "1991-01";
            _validator.Handle(_request, default(CancellationToken), () => Unit.Task).Wait();
            AssertValidationFailed();
            AssertContainsError(ValidationRuleConstant.ReportVersionNotFoundErrorCode);
        }

        private void AssertValidationFailed()
        {
            Assert.True(_request.IsFileValid, "File is not valid");
            Assert.False(_request.IsModelValid, "Model is valid");
            Assert.True(_request.IsReportValid, "Report is not valid");
        }

        private void AssertValidationPassed()
        {
            Assert.True(_request.IsFileValid, "File is not valid");
            Assert.True(_request.IsModelValid, "Model is not valid");
            Assert.True(_request.IsReportValid, "Report is not valid");
            Assert.DoesNotContain(_request.ProcessingResult, p => p.Severity == ValidationRuleSeverity.Error);
        }

        private void AssertContainsError(string errorCode)
        {
            Assert.Contains(_request.ProcessingResult,p => p.ValidationId == errorCode);
        }
    }
}
