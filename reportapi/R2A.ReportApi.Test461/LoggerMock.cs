using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NLog;
using R2A.ReportApi.Service.Infrastructure;

namespace R2A.ReportApi.Test461
{
    public class LogFactoryMock : ILogFactory
    {
        public ILogger GetLogger<T>() where T : class
        {
            var mock = new Mock<ILogger>();
            
            return mock.Object;
        }
    }
    
}
