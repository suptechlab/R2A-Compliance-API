using BSP.ReportDataConverter.Implementations.DynamicFields;
using R2A.ReportApi.Service.Infrastructure;

namespace R2A.ReportApi.Service.ReportSubmission
{
    public class DynamicOptionsRepositoryAdapter : DynamicOptionsRepository
    {
        public DynamicOptionsRepositoryAdapter(Settings settings): base(settings.DbConnectionString) { }
    }
}
