using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Xml.Schema;
using BSP.ReportDataConverter.Implementations.Warehouse.Engine;
using NLog;
using Pinecone.ReportDataConverter.Config;
using Pinecone.SqlCommandExtensions;
using R2A.ReportApi.Service.Infrastructure;
using R2A.ReportApi.Service.Model;

namespace R2A.ReportApi.Service.ReportSubmission.ReportConfiguration
{
    public class ReportDefinitionCacheManager
    {
        private static readonly ConcurrentDictionary<int, ReportDefinitionData> ReportDefinitionCache =
            new ConcurrentDictionary<int, ReportDefinitionData>();

        private static readonly Dictionary<int, object> ReportDefinitionCacheLocks =
            new Dictionary<int, object>();

        private const string ReportDefinitionSql =
            @"SELECT r.Id AS ReportId, rv.Id AS ReportVersionId, r.Code AS ReportCode, r.RecurrenceType, rv.JsonDefinition, rv.XsdDefinition, rv.XsdNamespace 
              FROM dbo.ReportVersion AS rv
              INNER JOIN dbo.Report AS r ON r.Id = rv.ReportId
              WHERE rv.Id = @id";

        private const string BankDomesticFlag =
            @"SELECT b.IsDomestic FROM bsp.Bank AS b WHERE b.Code = @code";

        private const string ReportValidationsSql =
            @"SELECT * FROM dbo.ReportValidation AS rv WHERE rv.ReportVersionId = @id ORDER BY Code";

        private static readonly ILogger _logger = NLog.LogManager.GetCurrentClassLogger();

        public static bool GetBankDomesticFlag(string bankCode, Settings settings)
        {
            using (var conn = new SqlConnection(settings.DbConnectionString))
            {
                conn.Open();
                var command = new SqlCommand(BankDomesticFlag, conn);
                command.Parameters.Add("@code", SqlDbType.NVarChar, bankCode.Length).Value = bankCode;
                command.Prepare();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetValue<bool>("IsDomestic");
                    }
                }
            }

            return false;
        }
        
        private static object GetReportDefinitionCacheLock(int reportVersionId)
        {
            lock (ReportDefinitionCacheLocks)
            {
                if (!ReportDefinitionCacheLocks.TryGetValue(reportVersionId, out var lockObject))
                {
                    lockObject = new object();
                    ReportDefinitionCacheLocks.Add(reportVersionId, lockObject);
                }

                return lockObject;
            }
        }

        public static ReportDefinitionData GetConfiguration(int reportVersionId, Settings settings, CultureInfo locale)
        {
            if (!ReportDefinitionCache.TryGetValue(reportVersionId, out var reportDefinitionData))
            {
                var lockObject = GetReportDefinitionCacheLock(reportVersionId);
                lock (lockObject)
                {
                    if (!ReportDefinitionCache.TryGetValue(reportVersionId, out reportDefinitionData))
                    {
                        int reportId;
                        string reportCode;
                        string jsonDefinitionPath;
                        string xsdDefinitionPath;
                        string xmlNamespace;
                        string recurrenceType;
                        List<ReportValidationFormula> reportValidationFormulas;

                        try
                        {
                            using (var conn = new SqlConnection(settings.DbConnectionString))
                            {
                                conn.Open();


                                var command = new SqlCommand(ReportDefinitionSql, conn);
                                command.Parameters.AddParameter("@id", reportVersionId, SqlDbType.Int);
                                command.Prepare();
                                using (var reader = command.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        reportId = reader.GetValue<int>("ReportId");
                                        reportCode = reader.GetString("ReportCode");
                                        jsonDefinitionPath = reader.GetString("JsonDefinition");
                                        xsdDefinitionPath = reader.GetString("XsdDefinition");
                                        xmlNamespace = reader.GetString("XsdNamespace");
                                        recurrenceType = reader.GetString("RecurrenceType");
                                    }
                                    else
                                    {
                                        return null;
                                    }
                                }

                                reportValidationFormulas = LoadValidationFormulas(reportVersionId, conn, locale);
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.Warn(e,
                                $"Exception was thrown while loading configuration for ReportVersionId {reportVersionId}.");
                            return null;
                        }


                        var reportConfig =
                            LoadReportConfig(jsonDefinitionPath, settings.UseBinaryJsonCache, xmlNamespace);

                        reportValidationFormulas.ForEach(rvf => rvf.ProcessFormulas());

                        var xmlSchemaSet = new XmlSchemaSet();
                        xmlSchemaSet.Add(xmlNamespace, xsdDefinitionPath);

                        var formWarehouseConfigs = MappingConfigBuilder.BuildMapping(reportConfig, settings.DbConnectionString);

                        reportDefinitionData = new ReportDefinitionData(reportId, reportVersionId, reportCode, recurrenceType,
                            xmlNamespace,
                            xmlSchemaSet,
                            reportConfig,
                            reportValidationFormulas,
                            formWarehouseConfigs);

                        ReportDefinitionCache.AddOrUpdate(reportVersionId, reportDefinitionData,
                            (key, oldValue) => reportDefinitionData);
                    }
                }
            }

            return reportDefinitionData;
        }


        private static List<ReportValidationFormula> LoadValidationFormulas(int reportVersionId, SqlConnection conn, IFormatProvider formatProvider = null)
        {
            var reportValidationFormulas = new List<ReportValidationFormula>();
            var command = new SqlCommand(ReportValidationsSql, conn);
            command.Parameters.AddParameter("@id", reportVersionId, SqlDbType.Int);
            command.Prepare();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = reader.GetValue<int>("Id");
                    var code = reader.GetString("Code");
                    var description = reader.GetString("Description");
                    var additionalDescription = reader.GetString("AdditionalDescription");
                    var operatorStr = reader.GetString("Operator");
                    var severity = reader.GetValue<int>("Severity");
                    var leftFormula = reader.GetString("LeftFormula");
                    var rightFormula = reader.GetString("RightFormula");
                    var conditionFormula = reader.GetString("FormulaCondition");
                    var tolerance = reader.GetNullable<decimal>("Tolerance");
                    var requiredTemplatesLeft = reader.GetString("RequiredTemplatesLeft");
                    var requiredTemplatesRight = reader.GetString("RequiredTemplatesRight");
                    var userFriendlyFormula = reader.GetString("UserFriendlyFormula");
                    var active = reader.GetValue<bool>("Active");

                    reportValidationFormulas.Add(
                        new ReportValidationFormula(id, code, description, additionalDescription, severity, leftFormula,
                            rightFormula, operatorStr, tolerance, conditionFormula, requiredTemplatesLeft, requiredTemplatesRight,
                            userFriendlyFormula, active, formatProvider)
                    );
                }
            }

            return reportValidationFormulas;
        }


        private static ReportConfig LoadReportConfig(string jsonDefinitionPath, bool useBinaryJsonCache,
            string xmlNamespace)
        {
            ReportConfig reportConfig;
            if (useBinaryJsonCache && File.Exists(jsonDefinitionPath + ".bin"))
            {
                reportConfig = ReportConfig.Deserialize(new FileInfo(jsonDefinitionPath + ".bin"));
            }
            else
            {
                reportConfig = ConfigFactoryDpmV2.LoadReportConfigFromFile(jsonDefinitionPath, xmlNamespace);
                if (useBinaryJsonCache)
                {
                    ReportConfig.Serialize(new FileInfo(jsonDefinitionPath + ".bin"), reportConfig);
                }
            }

            return reportConfig;
        }
    }
}