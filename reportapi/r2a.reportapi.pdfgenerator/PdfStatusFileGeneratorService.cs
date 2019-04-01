using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.Rendering;
using PdfSharp.Pdf;

namespace R2A.ReportApi.PdfGenerator
{
    public class PdfStatusFileGeneratorService
    {
        private const string StatusDescriptionStyleName = "StatusDescriptionStyle";
        private const int MaxCharsInDescriptionText = 83;
        private readonly string _logoImagePath;

        public PdfStatusFileGeneratorService(string logoImagePath)
        {
            _logoImagePath = logoImagePath;
        }

        public void GenerateReportSubmitionConfirmation(string bankName, DateTime submitTimestamp, string reportName, string periodText,
            string statusText, IEnumerable<StatusDescriptionItem> statusDescription, Stream destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination),"The output stream must not be null.");
            }

            if (!destination.CanWrite)
            {
                throw new ArgumentException("The output stream must be open and able to be written into.",nameof(destination));
            }

            if (!File.Exists(_logoImagePath))
                throw new FileNotFoundException("Resources were not found in the designated location.");

            Document document = new Document();
            document.Info.Title = "Report submission confirmation";
            document.Info.Subject = String.Format("{0}, {1} - {2}", bankName, reportName, periodText);
            document.Info.Author = "Bangko Sentral ng Pilipinas";

            document.DefaultPageSetup.PageFormat = PageFormat.A4;

            document.DefaultPageSetup.TopMargin = Unit.FromMillimeter(60);
            document.DefaultPageSetup.LeftMargin = Unit.FromMillimeter(24);
            document.DefaultPageSetup.BottomMargin = Unit.FromMillimeter(36);
            document.DefaultPageSetup.RightMargin = Unit.FromMillimeter(24);
            document.DefaultPageSetup.HeaderDistance = Unit.FromMillimeter(5);
            document.DefaultPageSetup.FooterDistance = Unit.FromMillimeter(5);

            Style normalStyle = document.Styles[StyleNames.Normal];
            normalStyle.Font.Name = "Arial";
            normalStyle.Font.Size = 11;
            normalStyle.ParagraphFormat.LineSpacing = 1.1;
            normalStyle.ParagraphFormat.LineSpacingRule = LineSpacingRule.Multiple;
            normalStyle.ParagraphFormat.SpaceAfter = 20;
            normalStyle.ParagraphFormat.SpaceBefore = 20;
            normalStyle.ParagraphFormat.KeepTogether = true;

            Style headerStyle = document.Styles[StyleNames.Header];
            headerStyle.ParagraphFormat.Alignment = ParagraphAlignment.Center;
            //headerStyle.ParagraphFormat.RightIndent = Unit.FromMillimeter(88);
            headerStyle.ParagraphFormat.LineSpacing = Unit.FromMillimeter(5.3);
            headerStyle.ParagraphFormat.LineSpacingRule = LineSpacingRule.AtLeast;
            headerStyle.ParagraphFormat.SpaceAfter = 0;

            Style footerStyle = document.Styles[StyleNames.Footer];
            footerStyle.ParagraphFormat.Alignment = ParagraphAlignment.Left;
            footerStyle.Font.Size = 8.5;
            footerStyle.ParagraphFormat.SpaceAfter = 0;

            Style heading1Style = document.Styles[StyleNames.Heading1];
            heading1Style.Font.Size = 14;
            heading1Style.Font.Bold = true;
            heading1Style.ParagraphFormat.Alignment = ParagraphAlignment.Center;
            heading1Style.ParagraphFormat.SpaceBefore = 12;

            Style statusDescriptionStyle = document.AddStyle(StatusDescriptionStyleName, StyleNames.Normal);
            statusDescriptionStyle.Font.Name = "Consolas";
            statusDescriptionStyle.Font.Size = 10;
            statusDescriptionStyle.ParagraphFormat.KeepTogether = false;
            statusDescriptionStyle.ParagraphFormat.SpaceAfter = 3;
            statusDescriptionStyle.ParagraphFormat.SpaceBefore = 0;
            

            Section section = document.AddSection();
            section.PageSetup.PageFormat = PageFormat.A4;

            section.PageSetup.TopMargin = Unit.FromMillimeter(60);
            section.PageSetup.LeftMargin = Unit.FromMillimeter(24);
            section.PageSetup.BottomMargin = Unit.FromMillimeter(36);
            section.PageSetup.RightMargin = Unit.FromMillimeter(24);
            section.PageSetup.HeaderDistance = Unit.FromMillimeter(5);
            section.PageSetup.FooterDistance = Unit.FromMillimeter(5);



            TextFrame pageNoFrame = section.Headers.Primary.AddTextFrame();
            pageNoFrame.RelativeHorizontal = RelativeHorizontal.Page;
            pageNoFrame.RelativeVertical = RelativeVertical.Page;
            pageNoFrame.WrapFormat.DistanceTop = Unit.FromMillimeter(33);
            pageNoFrame.Left = ShapePosition.Right;
            pageNoFrame.Width = Unit.FromMillimeter(100);
            pageNoFrame.MarginRight = Unit.FromMillimeter(24);

            Paragraph pageNoParagraph = pageNoFrame.AddParagraph();
            pageNoParagraph.AddPageField();
            pageNoParagraph.AddText(" / ");
            pageNoParagraph.AddNumPagesField();
            pageNoParagraph.Format.Alignment = ParagraphAlignment.Right;

            Paragraph headerParagraph = section.Headers.Primary.AddParagraph();


            Image headerImage = headerParagraph.AddImage(_logoImagePath);
            //headerImage.Width = Unit.FromMillimeter(20);
            headerImage.LockAspectRatio = true;
            headerImage.WrapFormat.Style = WrapStyle.TopBottom;

            /*
            Paragraph footerParagraph = section.Footers.Primary.AddParagraph();
            footerParagraph.AddText(
                "ADDRESS GOES HERE");
            footerParagraph.AddLineBreak();
            footerParagraph.AddText(
                "CONTACT INFO GOES HERE, www.bsp.gov.ph");
                */

            section.AddParagraph("Submission confirmation", StyleNames.Heading1);

            Paragraph text = section.AddParagraph();
            text.AddText("This document confirms that undertaking ");
            text.AddFormattedText(bankName, TextFormat.Bold);
            text.AddText(" on ");
            text.AddFormattedText(submitTimestamp.ToString("dd MMM yyyy"), TextFormat.Bold);
            text.AddText(" at ");
            text.AddFormattedText(submitTimestamp.ToString("h:mm:ss tt"), TextFormat.Bold);
            text.AddText(" submitted a report ");
            text.AddFormattedText(reportName, TextFormat.Bold);
            text.AddText(" for the submission period ");
            text.AddText(periodText);
            text.AddText(".");

            Paragraph status = section.AddParagraph();
            status.AddFormattedText("Report status: ",TextFormat.Bold);
            status.AddText(statusText);

            foreach (var descItem in statusDescription)
            {
                var statusHeaderParagraph = section.AddParagraph("",StatusDescriptionStyleName);
                statusHeaderParagraph.Format.KeepTogether = true;
                statusHeaderParagraph.AddLineBreak();
                statusHeaderParagraph.AddFormattedText(descItem.Header, TextFormat.Bold | TextFormat.Underline);
                if (!string.IsNullOrEmpty(descItem.HeaderAdditionalInfo))
                {
                    statusHeaderParagraph.AddText($"  {descItem.HeaderAdditionalInfo}");
                }
                statusHeaderParagraph.AddLineBreak();
                statusHeaderParagraph.AddText(descItem.Description);

                if (descItem.Details.Any())
                {
                    var bodyParagraph = section.AddParagraph("", StatusDescriptionStyleName);
                    foreach (var detail in descItem.Details)
                    {
                        bodyParagraph.AddLineBreak();
                        bodyParagraph.AddFormattedText(detail.Name + ": ", TextFormat.Bold);

                        var separateBySpace = detail.Text.Split(new []{' '},StringSplitOptions.RemoveEmptyEntries).ToList();
                        int currentLineLength = 0;
                        for (int i = 0; i < separateBySpace.Count; i++)
                        {
                            var word = separateBySpace[i];
                            if (string.IsNullOrEmpty(word))
                                continue;
                            if (currentLineLength + word.Length + 1 <= MaxCharsInDescriptionText)
                            {
                                currentLineLength += word.Length + 1;
                                bodyParagraph.AddText(word + " ");
                            }
                            else if (word.Length <= MaxCharsInDescriptionText)
                            {
                                currentLineLength = word.Length + 1;
                                bodyParagraph.AddText(word + " ");
                            }
                            else
                            {
                                var part1 = word.Substring(0, MaxCharsInDescriptionText - currentLineLength);
                                bodyParagraph.AddText(part1);
                                separateBySpace.Insert(i+1,word.Substring(MaxCharsInDescriptionText - currentLineLength));
                                currentLineLength = 0;
                            }
                        }
                        
                    }
                }
            }

            var pdfRenderer = new PdfDocumentRenderer(true, PdfFontEmbedding.Always);
            pdfRenderer.Document = document;
            pdfRenderer.RenderDocument();
            pdfRenderer.PdfDocument.Save(destination, false);
        }
    }
}
