using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;

namespace R2A.ReportApi.Service.Infrastructure
{
    public class MessageDumpService
    {
        private readonly Settings _settings;

        public MessageDumpService(Settings settings)
        {
            _settings = settings;
        }

        public async Task DumpMessage(string body, IDictionary<string,string> headers, Guid token)
        {
            using (var dumpFile = new StreamWriter(Path.Combine(_settings.MessageDumpFolder, $"msg_{token}.dmp"),false))
            {
                string dump = JsonConvert.SerializeObject(new
                {
                    body, headers, token
                });
                await dumpFile.WriteAsync(dump);
            }
        }
    }
}
