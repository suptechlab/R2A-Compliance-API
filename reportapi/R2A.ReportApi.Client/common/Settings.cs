using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace R2A.ReportApi.Client.Common
{
    public class Settings
    {
        public string DbConnectionString { get; set; }
        public string CertificateNamePrefix { get; set; }
        public string MqRoutingId { get; set; }
        public string MqExchange { get; set; }
    }
}
