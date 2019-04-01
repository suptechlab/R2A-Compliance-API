using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using R2A.ReportApi.Service.Infrastructure;
using R2A.ReportApi.Service.Model;
using R2A.ReportApi.Service.ReportSubmission.ReportConfiguration;
using Xunit;
using TimeSpan = System.TimeSpan;

namespace R2A.ReportApi.Test461
{
    public class CacheTest
    {
        [Fact]
        public void CacheBuildsOnceSequentially()
        {
            var settings = new Settings()
            {
                DbConnectionString =
                    "Data Source=10.100.93.6;initial catalog=R2A_S1A;persist security info=True;user id=r2a;password=r2a;MultipleActiveResultSets=True;",
                UseBinaryJsonCache = false
            };


            Assert.InRange(Time(() => ReportDefinitionCacheManager.GetConfiguration(1, settings, CultureInfo.InvariantCulture)), new TimeSpan(0), new TimeSpan(0,0,3,500));
            Assert.InRange(Time(() => ReportDefinitionCacheManager.GetConfiguration(1, settings, CultureInfo.InvariantCulture)),new TimeSpan(0),new TimeSpan(0,0,0,50));
        }
        [Fact]
        public void CacheBuildsOnceParralel()
        {
            var settings = new Settings()
            {
                DbConnectionString =
                    "Data Source=10.100.93.6;initial catalog=R2A_S1A;persist security info=True;user id=r2a;password=r2a;MultipleActiveResultSets=True;",
                UseBinaryJsonCache = false
            };
            
            var task1 =  new Task<TimeSpan>(() =>
                Time(() => ReportDefinitionCacheManager.GetConfiguration(1, settings, CultureInfo.InvariantCulture)));
            var task2 = new Task<TimeSpan>(() =>
                Time(() => ReportDefinitionCacheManager.GetConfiguration(1, settings, CultureInfo.InvariantCulture)));
            var task3 = new Task<TimeSpan>(() =>
                Time(() => ReportDefinitionCacheManager.GetConfiguration(1, settings, CultureInfo.InvariantCulture)));
            task1.Start();
            task2.Start();
            task3.Start();
            Task.WaitAll(task1, task2, task3);

            //shortest time is time it took the task which built the cache
            var shortestTime =
                new TimeSpan(Math.Min(Math.Min(task1.Result.Ticks, task2.Result.Ticks), task3.Result.Ticks));
            //the other tasks wait for the cache building task to finish and should just get its result
            //if the task did that and not rebuild the cache themselves, their time won't be much longer than the shortest one
            Assert.InRange(task1.Result-shortestTime,
                new TimeSpan(0), new TimeSpan(0,0,0,50));
            Assert.InRange(task2.Result - shortestTime,
                new TimeSpan(0), new TimeSpan(0, 0, 0, 50));
            Assert.InRange(task3.Result - shortestTime,
                new TimeSpan(0), new TimeSpan(0, 0, 0, 50));

        }

        private TimeSpan Time(Action toTime)
        {
            var timer = Stopwatch.StartNew();
            toTime();
            timer.Stop();
            return timer.Elapsed;
        }
    }
}
