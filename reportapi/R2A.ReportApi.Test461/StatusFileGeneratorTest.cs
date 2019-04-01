using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using R2A.ReportApi.Models;
using R2A.ReportApi.PdfGenerator;
using R2A.ReportApi.Service.Model;
using R2A.ReportApi.Service.ReportSubmission;
using R2A.ReportApi.Service.ReportSubmission.ReportStatus;
using Tester.Model;
using Xunit;

namespace R2A.ReportApi.Test461
{
    public class StatusFileGeneratorTest
    {
        [Fact]
        public void CanGeneratePdf()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-PH");
            var settings = new 
            {
                LogoImagePath = "D:\\Workspace\\BSP\\Projects\\R2A.ReportApi\\R2A.ReportApi.Service\\bsp-logo.png"
            };

            var pdfGenerator = new PdfStatusFileGeneratorService(settings.LogoImagePath);

            Stream file = new FileStream("testPdf.pdf", FileMode.Create);


            pdfGenerator.GenerateReportSubmitionConfirmation("BANK_NAME", DateTime.Now, "REPORT_NAME",
                "PERIOD_TEXT", "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Quisque consectetur semper urna, at sodales nunc scelerisque sed.",
                new[]
                {
                    new StatusDescriptionItem()
                    {
                        Header = "Header",
                        Description =
                            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Quisque consectetur semper urna, at sodales nunc scelerisque sed. Morbi molestie pulvinar leo vel molestie. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Integer nunc sapien, ultricies non mattis vel, sollicitudin maximus lectus.",
                        Details = new List<StatusDescriptionDetailItem>()
                        {
                            new StatusDescriptionDetailItem() {Name = "Name", Text = "Lorem_ipsum_dolor_sit_amet,_consectetur_adipiscing_elit._Quisque_consectetur_semper_urna,_at_sodales nunc_scelerisque_sed._Morbi_molestie_pulvinar_leo_vel_molestie._Vestibulum_ante_ipsum_primis_in_faucibus_orci_luctus_et_ultrices_posuere_cubilia_Curae;_Integer_nunc_sapien,_ultricies_non_mattis_vel,_sollicitudin_maximus_lectus."},
                            new StatusDescriptionDetailItem() {Name = "Name", Text = "Lorem_ipsum_dolor_sit_amet,_consectetur_adipiscing_elit._Quisque_consectetur_semper_urna,_at_sodales_nunc_scelerisque_sed._Morbi_molestie_pulvinar_leo_vel_molestie._Vestibulum_ante_ipsum_primis_in_faucibus_orci_luctus_et_ultrices_posuere_cubilia_Curae;_Integer_nunc_sapien,_ultricies non_mattis_vel,_sollicitudin_maximus_lectus."},
                            new StatusDescriptionDetailItem() {Name = "Name", Text = "Lorem_ipsum_dolor_sit_amet,_consectetur_adipiscing_elit._Quisque_consectetur_semper_urna,_at_sodales_nunc scelerisque_sed._Morbi_molestie_pulvinar_leo_vel_molestie._Vestibulum_ante_ipsum_primis_in_faucibus_orci_luctus_et_ultrices posuere_cubilia_Curae;_Integer_nunc_sapien,_ultricies_non_mattis_vel,_sollicitudin_maximus_lectus."}
                        }
                    },
                    new StatusDescriptionItem()
                    {
                        Header = "Header",
                        Description =
                            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Quisque consectetur semper urna, at sodales nunc scelerisque sed. Morbi molestie pulvinar leo vel molestie. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Integer nunc sapien, ultricies non mattis vel, sollicitudin maximus lectus.",
                        Details = new List<StatusDescriptionDetailItem>()
                        {
                            new StatusDescriptionDetailItem() {Name = "Name", Text = "Text"},
                            new StatusDescriptionDetailItem() {Name = "Name", Text = "Text"},
                            new StatusDescriptionDetailItem() {Name = "Name", Text = "Text"}
                        }
                    },
                    new StatusDescriptionItem()
                    {
                        Header = "Header",
                        Description =
                            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Quisque consectetur semper urna, at sodales nunc scelerisque sed. Morbi molestie pulvinar leo vel molestie. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Integer nunc sapien, ultricies non mattis vel, sollicitudin maximus lectus.",
                        Details = new List<StatusDescriptionDetailItem>()
                        {
                            new StatusDescriptionDetailItem() {Name = "Name", Text = "Text"},
                            new StatusDescriptionDetailItem() {Name = "Name", Text = "Text"},
                            new StatusDescriptionDetailItem() {Name = "Name", Text = "Text"}
                        }
                    },
                    new StatusDescriptionItem()
                    {
                        Header = "Header",
                        Description =
                            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Quisque consectetur semper urna, at sodales nunc scelerisque sed. Morbi molestie pulvinar leo vel molestie. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Integer nunc sapien, ultricies non mattis vel, sollicitudin maximus lectus.",
                        Details = new List<StatusDescriptionDetailItem>()
                        {
                            new StatusDescriptionDetailItem() {Name = "Name", Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Quisque consectetur semper urna, at sodales nunc scelerisque sed. Morbi molestie pulvinar leo vel molestie. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae;".Replace(' ','_')},
                            new StatusDescriptionDetailItem() {Name = "Name", Text = "Text"},
                            new StatusDescriptionDetailItem() {Name = "Name", Text = "Text"}
                        }
                    },
                    new StatusDescriptionItem()
                    {
                        Header = "Header",
                        Description =
                            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Quisque consectetur semper urna, at sodales nunc scelerisque sed. Morbi molestie pulvinar leo vel molestie. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Integer nunc sapien, ultricies non mattis vel, sollicitudin maximus lectus.",
                        Details = new List<StatusDescriptionDetailItem>()
                        {
                            new StatusDescriptionDetailItem() {Name = "Name", Text = "Text"},
                            new StatusDescriptionDetailItem() {Name = "Name", Text = "Text"},
                            new StatusDescriptionDetailItem() {Name = "Name", Text = "Text"}
                        }
                    }
                }, file);

            Assert.True(file.Length > 0,"File must not be empty");
            file.Dispose();
        }

        [Fact]
        public void GenerateSuccessPdf()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-PH");
            var settings = new
            {
                LogoImagePath = "D:\\Workspace\\BSP\\Projects\\R2A.ReportApi\\R2A.ReportApi.Service\\bsp-logo.png"
            };

            var pdfGenerator = new PdfStatusFileGeneratorService(settings.LogoImagePath);

            Stream file = new FileStream("testPdf.pdf", FileMode.Create);


            pdfGenerator.GenerateReportSubmitionConfirmation("BPI", DateTime.Now, "FRP",
                "2018-05", SubmissionStatus.Accepted.ToString(),
                new List<StatusDescriptionItem>(), file);

            Assert.True(file.Length > 0, "File must not be empty");
            file.Dispose();
        }

        [Fact]
        public void CanGenerateXml()
        {
            var request = new ReportSubmissionRequest(new ReportSubmissionDto()
            {
                ReportInfo = new ReportSubmissionInfo()
                {
                    ReportCode = "REPORT_CODE",
                    UndertakingId = "BANK_CODE",
                    PeriodInfo = "PERIOD_INFO"
                }
            }, new ReportStatusInfo()
            {
                Token = Guid.NewGuid(),
                TimeSubmitted = DateTime.Now,
            }, null);
            request.SubmissionInfo.SubmissionStatus = SubmittedReportStatus.Rejected;
            request.ProcessingResult.Add(ValidationRule.Error(ValidationRuleConstant.ZipCorruptedArchiveError));
            request.ProcessingResult.Add(ValidationRule.Create("VALIDATION_ID","DESCRIPTION","FORMULA","FORMULA_RESULT",ValidationRuleSeverity.Warning));
            XmlStatusFileGeneratorService.GenerateFileAsync(request, "testXml.xml",DateTime.Now);
        }
    }
}
