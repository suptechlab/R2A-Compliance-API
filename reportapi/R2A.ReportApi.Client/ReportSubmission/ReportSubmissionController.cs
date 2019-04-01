using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using R2A.ReportApi.Client.Authentication;
using R2A.ReportApi.Client.Common.Errors;
using R2A.ReportApi.Client.Infrastructure;

using R2A.ReportApi.Models;
using ServiceStack;

namespace R2A.ReportApi.Client.ReportSubmission
{

    
    [Route("api/report")]
    [Authorize(AuthenticationSchemes = Authentication.AuthenticationScheme.CertificateAuthentication)]
    public class ReportSubmissionController : Controller
    {
        private static readonly Regex MonthlyRegex = new Regex(@"^([0-9]{4})-(0[1-9]|1[012])$");

        private readonly IMediator _mediator;
        private readonly ILogger<ReportSubmissionController> _logger;


        public ReportSubmissionController(IMediator mediator, ILoggerFactory loggerFactory)
        {
            _mediator = mediator;
            _logger = loggerFactory.CreateLogger<ReportSubmissionController>();
        }

        [Consumes("application/json")]
        [Produces("application/json")]
        [HttpPost]
        public async Task<ActionResult> SubmitReport([FromBody] ReportSubmissionDto dto, CancellationToken cancellationToken)
        {
            List<ErrorResponse> errors = new List<ErrorResponse>();

            //validate dto
            if (dto == null)
            {
                errors.Add(new ErrorResponse()
                {
                    Code = ErrorResponseCode.ValidationReportInfo,
                    Meta = new BaseErrorMeta() { Message = "Request body is missing" }
                });
                return new BadRequestObjectResult(errors);
            }
            if (string.IsNullOrEmpty(dto.ReportFile))
            {
                errors.Add(new ErrorResponse()
                {
                    Code = ErrorResponseCode.ValidationReportFile,
                    Field = "reportFile",
                    Meta = new BaseErrorMeta() { Message = "Report file is missing" }
                });
            }
            if (dto.ReportInfo == null)
            {
                errors.Add(new ErrorResponse()
                {
                    Code = ErrorResponseCode.ValidationReportInfo,
                    Field = "reportInfo",
                    Meta = new BaseErrorMeta() { Message = "Report info is missing" }
                });
            }
            else
            {

                if (String.IsNullOrEmpty(dto.ReportInfo.PeriodInfo))
                {
                    errors.Add(new ErrorResponse() { Code = ErrorResponseCode.ValidationReportInfo, Field = "reportInfo.periodInfo", Meta = new BaseErrorMeta() { Message = "Period info is missing" } });
                }
                if(!MonthlyRegex.IsMatch(dto.ReportInfo.PeriodInfo))//TODO remove hardcoded validation
                {
                    errors.Add(new ErrorResponse() { Code = ErrorResponseCode.ValidationReportInfo, Field = "reportInfo.periodInfo", Meta = new BaseErrorMeta() { Message = $"Period info {dto.ReportInfo.PeriodInfo} is invalid" } });
                }

                if (String.IsNullOrEmpty(dto.ReportInfo.ReportCode))
                {
                    errors.Add(new ErrorResponse() { Code = ErrorResponseCode.ValidationReportInfo, Field = "reportInfo.reportCode", Meta = new BaseErrorMeta() { Message = "Report code is missing" } });
                }
                if (dto.ReportInfo.ReportCode!="FRP")//TODO remove hardcoded validation
                {
                    errors.Add(new ErrorResponse() { Code = ErrorResponseCode.ValidationReportInfo, Field = "reportInfo.reportCode", Meta = new BaseErrorMeta() { Message = $"Report code {dto.ReportInfo.ReportCode} is invalid" } });
                }

                if (string.IsNullOrEmpty(dto.ReportInfo.UndertakingId))
                {
                    errors.Add(new ErrorResponse() { Code = ErrorResponseCode.ValidationReportInfo, Field = "reportInfo.undertakingId", Meta = new BaseErrorMeta() { Message = "Bank code is missing" } });
                }

                if (dto.ReportInfo.UndertakingId != User.Claims.GetValueOfType(ClaimTypes.Sid))
                {
                    errors.Add(new ErrorResponse() { Code = ErrorResponseCode.ValidationReportInfo, Field = "reportInfo.undertakingId", Meta = new BaseErrorMeta() { Message = $"Bank code {dto.ReportInfo.UndertakingId} does not match certificate." } });
                }
            }
            if (errors.Any())
            {
                _logger.LogWarning($"Bad request: {errors.Join(", ")}");
                return new BadRequestObjectResult(errors);
            }
            //initiate submission process

            var result = await _mediator.Send(new SubmitReport.Command(dto, User.Claims.GetCertificateInfo()), cancellationToken);

            return new OkObjectResult(new { token = result.Token });
        }

        [Produces("application/json")]
        [HttpGet]
        [Route("{processToken}/status")]
        public async Task<ActionResult> GetStatus(Guid processToken, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetStatus.Query(processToken, User.Claims.GetValueOfType(ClaimTypes.Sid)), cancellationToken);

            if (result.Status == null)
            {
                return new NotFoundObjectResult(new ErrorResponse()
                {
                    Code = ErrorResponseCode.InvalidToken,
                    Meta = new BaseErrorMeta() {Message = $"Unable to find report status for token {processToken}"}
                });
            }

            return new OkObjectResult(result.Status);
        }

        [Produces("application/json","application/octet-stream")]
        [HttpGet]
        [Route("{processToken}/statusFile")]
        public async Task<ActionResult> GetStatusFile(Guid processToken, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetStatusFilePath.Query(processToken, User.Claims.GetValueOfType(ClaimTypes.Sid)), cancellationToken);

            if (string.IsNullOrEmpty(result.FilePath) || !System.IO.File.Exists(result.FilePath))
            {
                return new NotFoundObjectResult(new ErrorResponse()
                {
                    Code = ErrorResponseCode.InvalidToken,
                    Meta = new BaseErrorMeta() { Message = $"Unable to find report status file for token {processToken}" }
                });
            }

            var stream = new FileStream(result.FilePath, FileMode.Open);
            
            return File(stream,"application/octet-stream",Path.GetFileName(result.FilePath));
            
        }
        
        [Produces("application/json","application/octet-stream")]
        [HttpGet]
        [Route("{processToken}/statusFilePdf")]
        public async Task<ActionResult> GetStatusFilePdf(Guid processToken, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetStatusFilePath.Query(processToken, User.Claims.GetValueOfType(ClaimTypes.Sid)), cancellationToken);

            if (string.IsNullOrEmpty(result.FilePathPdf) || !System.IO.File.Exists(result.FilePathPdf))
            {
                return new NotFoundObjectResult(new ErrorResponse()
                {
                    Code = ErrorResponseCode.InvalidToken,
                    Meta = new BaseErrorMeta() { Message = $"Unable to find PDF report status file for token {processToken}" }
                });
            }

            var stream = new FileStream(result.FilePathPdf, FileMode.Open);
            
            return File(stream,"application/octet-stream",Path.GetFileName(result.FilePathPdf));
        }
        
        [Produces("application/json","application/octet-stream")]
        [HttpGet]
        [Route("{processToken}/excelFile")]
        public async Task<ActionResult> GetExcelFile(Guid processToken, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetStatusFilePath.Query(processToken, User.Claims.GetValueOfType(ClaimTypes.Sid)), cancellationToken);

            if (string.IsNullOrEmpty(result.FilePathExcel) || !System.IO.File.Exists(result.FilePathExcel))
            {
                return new NotFoundObjectResult(new ErrorResponse()
                {
                    Code = ErrorResponseCode.InvalidToken,
                    Meta = new BaseErrorMeta() { Message = $"Unable to find Excel report file for token {processToken}" }
                });
            }

            var stream = new FileStream(result.FilePathExcel, FileMode.Open);
            
            return File(stream,"application/octet-stream",Path.GetFileName(result.FilePathExcel));
        }
    }
}