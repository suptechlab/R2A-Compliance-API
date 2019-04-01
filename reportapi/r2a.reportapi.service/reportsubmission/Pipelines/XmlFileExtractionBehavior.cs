using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NLog;
using R2A.ReportApi.Service.Infrastructure;
using R2A.ReportApi.Service.Model;
using Tester.Model;

namespace R2A.ReportApi.Service.ReportSubmission.Pipelines
{
    public class XmlFileExtractionBehavior : IPipelineBehavior<ReportSubmissionRequest, Unit>
    {
        private readonly Settings _settings;
        private readonly ILogger _logger;

        public XmlFileExtractionBehavior(ILogFactory logFactory, Settings settings)
        {
            _settings = settings;
            _logger = logFactory.GetLogger<XmlFileExtractionBehavior>();
        }

        public async Task<Unit> Handle(ReportSubmissionRequest request, CancellationToken cancellationToken,
            RequestHandlerDelegate<Unit> next)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rawData = TryDecodeBase64(request);
            if (rawData != null)
            {
                using (var rawDataStream = new MemoryStream(rawData))
                {
                    TryZipDecompress(rawDataStream, request, out var isZipArchive);

                    if (!request.IsFileValid && !isZipArchive)
                    {
                        TryGZipDecompress(rawDataStream, request, out var isGZipArchive);

                        if (!request.IsFileValid && !isGZipArchive)
                        {
                            request.XmlFileContents = rawData;
                        }
                    }
                }
                if (request.IsFileValid)
                {
                    using (var file = new FileStream(Path.Combine(_settings.XmlFileSaveLocation,
                            FileNamingConvention.GenerateFileNameBase(request.SubmissionInfo.ReportCode,
                                request.SubmissionInfo.Undertaking,
                                request.SubmissionInfo.ReportPeriod, request.TimeSubmitted, request.Id) + ".xml")
                        , FileMode.Create))
                    {
                        await file.WriteAsync(request.XmlFileContents, 0, request.XmlFileContents.Length,
                            cancellationToken);
                    }

                    request.Base64EncodedFile = null;
                }
            }

            return await next();
        }


        private byte[] TryDecodeBase64(ReportSubmissionRequest request)
        {
            try
            {
                return Convert.FromBase64String(request.Base64EncodedFile);
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Unable to decode report file from Base64 string. Id: {request.Id}.");
                request.ProcessingResult.Add(ValidationRule.Error(ValidationRuleConstant.Base64DecodeError));
                return null;
            }
        }


        private void TryZipDecompress(Stream rawDataStream, ReportSubmissionRequest request,
            out bool isZipArchive)
        {
            rawDataStream.Position = 0;
            isZipArchive = false;
            try
            {
                using (var archive = new ZipArchive(rawDataStream, ZipArchiveMode.Read, leaveOpen:true))
                {
                    isZipArchive = true;
                    if (archive.Entries.Count != 1)
                    {
                        request.ProcessingResult.Add(ValidationRule.Error(ValidationRuleConstant.ZipEntryCountError));
                        return;
                    }

                    foreach (var entry in archive.Entries)
                    {
                        using (var stream = entry.Open())
                        {
                            using (var memStream = new MemoryStream())
                            {
                                stream.CopyTo(memStream);
                                request.XmlFileContents = memStream.ToArray();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (isZipArchive)
                {
                    _logger.Warn(e, $"Unable to decompress zip file. Id: {request.Id}.");
                    request.ProcessingResult.Add(ValidationRule.Error(ValidationRuleConstant.ZipCorruptedArchiveError));
                }

                request.XmlFileContents = null;
            }
        }

        private void TryGZipDecompress(Stream rawDataStream, ReportSubmissionRequest request,
            out bool isGZipArchive)
        {
            rawDataStream.Position = 0;
            try
            {
                using (var stream = new GZipStream(rawDataStream, CompressionMode.Decompress, leaveOpen: true))
                {
                    using (var memStream = new MemoryStream())
                    {
                        stream.CopyTo(memStream);
                        request.XmlFileContents = memStream.ToArray();
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Warn(e, $"Unable to G-zip decompress zip file. Id: {request.Id}.");
                request.XmlFileContents = null;
            }

            isGZipArchive = request.IsFileValid;
        }
    }
}