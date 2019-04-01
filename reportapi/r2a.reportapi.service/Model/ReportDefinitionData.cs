using System.Collections.Generic;
using System.Xml.Schema;
using BSP.ReportDataConverter.Implementations.Warehouse.Mapping;
using Pinecone.ReportDataConverter.Config;

namespace R2A.ReportApi.Service.Model
{
    public class ReportDefinitionData
    {
        public int ReportId { get; }
        public int ReportVersionId { get; }
        public string ReportCode { get; }
        public string RecurrenceType { get; }
        public string XmlNamespace { get; }
        public XmlSchemaSet XmlSchemaSet { get; }
        public ReportConfig ReportConfig { get; }
        public List<FormWarehouseConfig> FormWarehouseConfigs { get;}
        
        public List<ReportValidationFormula> ValidationFormulas { get; }

        public ReportDefinitionData(int reportId, int reportVersionId, string reportCode, string recurrenceType, string xmlNamespace, XmlSchemaSet xmlSchemaSet, ReportConfig reportConfig, List<ReportValidationFormula> validationFormulas, List<FormWarehouseConfig> formWarehouseConfigs)
        {
            ReportId = reportId;
            ReportVersionId = reportVersionId;
            ReportCode = reportCode;
            RecurrenceType = recurrenceType;
            XmlNamespace = xmlNamespace;
            XmlSchemaSet = xmlSchemaSet;
            ReportConfig = reportConfig;
            ValidationFormulas = validationFormulas;
            FormWarehouseConfigs = formWarehouseConfigs;
        }
    }
}