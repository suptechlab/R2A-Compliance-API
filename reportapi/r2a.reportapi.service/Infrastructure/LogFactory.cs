using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace R2A.ReportApi.Service.Infrastructure
{
    public class LogFactory : ILogFactory
    {
        
        public ILogger GetLogger<T>() where T:class
        {   
            return NLog.LogManager.GetLogger(typeof(T).FullName);
        }
    }

    public interface ILogFactory
    {
        ILogger GetLogger<T>() where T : class;
    }

}
