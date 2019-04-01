using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace R2A.ReportApi.Models
{
    public static class MessageHeaders
    {
        public const string Token = nameof(Token);
        public const string Subject = nameof(Subject);
        public const string Issuer = nameof(Issuer);
        public const string Thumbprint = nameof(Thumbprint);

        public static IDictionary<string, object> ToRabbitMqHeaders(IDictionary<string, string> headers)
        {
            return headers.ToDictionary(p => p.Key, p => (object)Encoding.UTF8.GetBytes(p.Value));
        }

        public static IDictionary<string, string> FromRabbitMqHeaders(IDictionary<string, object> headers)
        {
            return headers.ToDictionary(p => p.Key, p => Encoding.UTF8.GetString((byte[])p.Value));
        }
    }

}
