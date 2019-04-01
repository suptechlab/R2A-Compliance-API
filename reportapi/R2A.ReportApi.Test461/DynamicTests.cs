using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSP.ReportDataConverter.Implementations.DynamicFields;

using Xunit;

namespace R2A.ReportApi.Test461
{
    public class DynamicTests
    {
        [Fact]
        public void GetsAllDynamicDropdowns()
        {
            var resolver = new ServiceDynamicDataResolver(new DynamicOptionsService(
                new DynamicOptionsRepository(
                    "Data Source=10.100.93.6;initial catalog=R2A_S1A;persist security info=True;user id=r2a;password=r2a;MultipleActiveResultSets=True;")));

            var sourceTypes = new[] {"SUBSIDIARY","BORROWER","BRANCH","BRANCH_DOM"};

            foreach (var sourceType in sourceTypes)
            {
                var res1 = resolver.GetAllDropDownValuesForSource(sourceType, null);
                var res2 = resolver.GetDropDownValues(new Pinecone.ReportDataConverter.Config.DynamicDropdownSource.Value(sourceType,null));

                var query = new Pinecone.ReportDataConverter.Config.DynamicDropdownSource.Value(sourceType, null);
                query.HeaderValues.Add(new Tuple<string, object>("Undertaking","1001.a"));
                query.HeaderValues.Add(new Tuple<string, object>("R1C1", 1));
                var res3 = resolver.GetDropDownValues(query);

                Assert.True(res1.Any(),$"{sourceType} all is empty");
                Assert.False(res2.Any(),$"{sourceType} with empty query is not empty");
                Assert.True(res3.Any(),$"{sourceType} with query is empty");
            }
            
        }
    }
}