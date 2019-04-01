using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
    public class XmlFileExtractionTests
    {
        private void TestSetup(string base64)
        {
            _request = new ReportSubmissionRequest(new ReportSubmissionDto()
            {
                ReportInfo = new ReportSubmissionInfo()
                {
                    ReportCode = "FRP",
                    UndertakingId = "1001.a",
                    PeriodInfo = "2018-03"
                },
                ReportFile = base64
            }, new ReportStatusInfo()
            {
            }, new Dictionary<string, string>()
            {
            });

            _validator = new XmlFileExtractionBehavior(new LogFactoryMock(), new Settings()
            {
                DbConnectionString =
                    "Data Source=10.100.93.6;initial catalog=R2A_S1A;persist security info=True;user id=r2a;password=r2a;MultipleActiveResultSets=True;",
                XmlFileSaveLocation = @"C:\Temp"
            });
        }

        private ReportSubmissionRequest _request;
        private XmlFileExtractionBehavior _validator;

        [Fact]
        public void NoFileFails()
        {
            TestSetup(null);
            RunTest();
            AssertValidationFailed();
            AssertContainsError(ValidationRuleConstant.Base64DecodeError.Code);
        }

        [Fact]
        public void DecodesBase64File()
        {
            TestSetup(ReadBase64(@"D:\Workspace\BSP\Tests\valid.xml"));
            RunTest();
            AssertValidationPassed();
        }

        [Fact]
        public void FailNonBase64()
        {
            TestSetup("čšćđž");
            RunTest();
            AssertValidationFailed();
            AssertContainsError(ValidationRuleConstant.Base64DecodeError.Code);
        }

        [Fact]
        public void UnzipsRegularZip()
        {
            TestSetup(ReadBase64(@"D:\Workspace\BSP\Tests\valid.zip"));
            RunTest();
            AssertValidationPassed();
        }

        [Fact]
        public void FailsZipOfMultipleFiles()
        {
            TestSetup(ReadBase64(@"D:\Workspace\BSP\Tests\invalid.zip"));
            RunTest();
            AssertValidationFailed();
            AssertContainsError(ValidationRuleConstant.ZipEntryCountError.Code);
        }

        [Fact]
        public void IgnoresCorruptZip()
        {
            TestSetup(ReadBase64(@"D:\Workspace\BSP\Tests\corrupt.zip"));
            RunTest();
            AssertValidationPassed();
        }

        [Fact]
        public void UnzipsRegularGZip()
        {
            TestSetup(ReadBase64(@"D:\Workspace\BSP\Tests\valid.gz"));
            RunTest();
            AssertValidationPassed();
        }

        [Fact]
        public void IgnoresCorruptGZip()
        {
            TestSetup(ReadBase64(@"D:\Workspace\BSP\Tests\corrupt.gz"));
            RunTest();
            AssertValidationPassed();
        }


        private string ReadBase64(string path)
        {
            return Convert.ToBase64String(File.ReadAllBytes(path));
        }

        private void AssertValidationFailed()
        {
            Assert.False(_request.IsFileValid, "File is valid");
            Assert.True(_request.IsModelValid, "Model is not valid");
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
            Assert.Contains(_request.ProcessingResult, p => p.ValidationId == errorCode);
        }

        private void RunTest()
        {
            _validator.Handle(_request, default(CancellationToken), () => Unit.Task).Wait();
        }
    }
}