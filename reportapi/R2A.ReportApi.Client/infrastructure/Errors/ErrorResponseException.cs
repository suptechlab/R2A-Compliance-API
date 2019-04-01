using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using R2A.ReportApi.Client.Common.Errors;

namespace R2A.ReportApi.Client.Infrastructure.Errors
{
    public class ErrorResponseException : OperationCanceledException
    {
        public IEnumerable<ErrorResponse> Errors { get; private set; }

        public int ResponseStatusCode { get; protected set; }

        public ErrorResponseException(ErrorResponse error)
            : this(ErrorResponse.Single(error))
        {
        }

        public ErrorResponseException(IEnumerable<ErrorResponse> errors)
            : this(errors, StatusCodes.Status500InternalServerError)
        {
        }

        public ErrorResponseException(ErrorResponse error, int statusCode)
            : this(ErrorResponse.Single(error), statusCode)
        {
        }

        public ErrorResponseException(IEnumerable<ErrorResponse> errors, int statusCode)
            : base(
                $"An operation reported an error that prevents further execution of the request. Errors:\n{JsonConvert.SerializeObject(errors)}")
        {
            Errors = errors;
            ResponseStatusCode = statusCode;
        }
    }
}