using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace R2A.ReportApi.Service.Model
{
    public class ReportingPeriod
    {
        private static readonly Regex NoneRegex = new Regex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2} [0-9]{2}:[0-9]{2}:[0-9]{2}$");
        private static readonly Regex DailyRegex = new Regex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}$");
        private static readonly Regex WeeklyRegex = new Regex(@"^([0-9]{4})-([0-5][0-9])$");
        private static readonly Regex MonthlyRegex = new Regex(@"^([0-9]{4})-(0[1-9]|1[012])$");
        private static readonly Regex QuarterlyRegex = new Regex(@"^([0-9]{4})-([1-4]{1})$");
        private static readonly Regex HalfYearlyRegex = new Regex(@"^([0-9]{4})-([1-2]{1})$");
        private static readonly Regex YearlyRegex = new Regex(@"^[0-9]{4}$");

        public static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            DateTime firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekOfYear;
            if (firstWeek <= 1)
            {
                weekNum -= 1;
            }

            var result = firstThursday.AddDays(weekNum * 7);
            return result.AddDays(-3);
        }

        public static int GetWeekCount(int year)
        {
            var startDate = new DateTime(year, 1, 1);
            switch (startDate.DayOfWeek)
            {
                case DayOfWeek.Wednesday:
                    if (DateTime.IsLeapYear(year))
                    {
                        return 53;
                    }

                    break;
                case DayOfWeek.Thursday:
                    return 53;
            }

            return 52;
        }

        public ReportingPeriodType? Type { get; private set; }
        public DateTime StartingDate { get; private set; }
        public DateTime EndingDate { get; private set; }
        public string PeriodInfo { get; private set; }
        public string PeriodTranslation { get; private set; }
        public int? Year { get; set; }
        public int? Period { get; set; }


        public ReportingPeriod(string periodInfo, ReportingPeriodType type)
        {
            InitFromStringAndType(periodInfo, type);
        }

        public ReportingPeriod(string periodInfo, char type)
        {
            InitFromStringAndType(periodInfo, ReportingPeriodTypeExtensions.From(type));
        }


        private void InitFromStringAndType(string periodInfo, ReportingPeriodType type)
        {
            PeriodInfo = periodInfo;
            Match match;
            Year = null;
            Period = null;
            switch (type)
            {
                case ReportingPeriodType.Daily:
                    match = DailyRegex.Match(periodInfo);
                    if (match.Success)
                    {
                        try
                        {
                            EndingDate = DateTime.ParseExact(periodInfo, "yyyy-MM-dd", null);
                            StartingDate = EndingDate;
                            PeriodTranslation = $"Day {EndingDate.ToString("dd. MM. yyyy.")} ";
                            Year = StartingDate.Year;
                            Period = null;
                        }
                        catch (Exception e)
                        {
                            match = null;
                        }
                    }

                    break;
                case ReportingPeriodType.Weekly:
                    match = WeeklyRegex.Match(periodInfo);
                    if (match.Success)
                    {
                        int year = int.Parse(match.Groups[1].Value);
                        int week = int.Parse(match.Groups[2].Value);
                        Year = year;
                        Period = week;
                        if ((week > 0) && (week <= GetWeekCount(year)))
                        {
                            try
                            {
                                StartingDate = FirstDateOfWeekISO8601(year, week);
                                EndingDate = StartingDate.AddDays(6);
                                PeriodTranslation = $"Week {week}/{year}";
                            }
                            catch (Exception e)
                            {
                                match = null;
                            }
                        }
                        else
                        {
                            match = null;
                        }
                    }

                    break;
                case ReportingPeriodType.Monthly:
                    match = MonthlyRegex.Match(periodInfo);
                    if (match.Success)
                    {
                        int year = int.Parse(match.Groups[1].Value);
                        int month = int.Parse(match.Groups[2].Value);
                        Year = year;
                        Period = month;
                        if (month > 0 && month <= 12)
                        {
                            try
                            {
                                StartingDate = new DateTime(year, month, 1);
                                EndingDate = StartingDate.AddMonths(1).AddDays(-1);
                                PeriodTranslation = $"Month {month}/{year}";
                            }
                            catch (Exception e)
                            {
                                match = null;
                            }
                        }
                        else
                        {
                            match = null;
                        }
                    }

                    break;
                case ReportingPeriodType.Quarterly:
                    match = QuarterlyRegex.Match(periodInfo);
                    if (match.Success)
                    {
                        int year = int.Parse(match.Groups[1].Value);
                        int quarter = int.Parse(match.Groups[2].Value);
                        Year = year;
                        Period = quarter;
                        try
                        {
                            StartingDate = new DateTime(year, 1 + (quarter - 1) * 3, 1);
                            EndingDate = StartingDate.AddMonths(3).AddDays(-1);
                            PeriodTranslation = $"Querter {quarter}/{year}";
                        }
                        catch (Exception e)
                        {
                            match = null;
                        }
                    }

                    break;
                case ReportingPeriodType.HalfYearly:
                    match = HalfYearlyRegex.Match(periodInfo);
                    if (match.Success)
                    {
                        int year = int.Parse(match.Groups[1].Value);
                        int half = int.Parse(match.Groups[2].Value);
                        Year = year;
                        Period = half;
                        try
                        {
                            StartingDate = new DateTime(year, 1 + (half - 1) * 6, 1);
                            EndingDate = StartingDate.AddMonths(6).AddDays(-1);
                            PeriodTranslation = $"{half}. semester of {year}";
                        }
                        catch (Exception e)
                        {
                            match = null;
                        }
                    }

                    break;
                case ReportingPeriodType.Yearly:
                    match = YearlyRegex.Match(periodInfo);
                    if (match.Success)
                    {
                        int year = int.Parse(periodInfo);
                        Year = year;
                        Period = null;
                        try
                        {
                            EndingDate = new DateTime(year, 12, 31);
                            StartingDate = new DateTime(year, 1, 1);
                            PeriodTranslation = String.Format("year {0}", year);
                        }
                        catch (Exception e)
                        {
                            match = null;
                        }
                    }

                    break;
                case ReportingPeriodType.None:
                    match = NoneRegex.Match(periodInfo);
                    Year = null;
                    Period = null;
                    if (match.Success)
                    {
                        try
                        {
                            EndingDate = DateTime.ParseExact(periodInfo, "yyyy-MM-dd HH:mm:ss", null);
                            StartingDate = EndingDate;
                            PeriodTranslation =
                                $"dan {EndingDate.ToString("dd. MM. yyyy.")} v {EndingDate.ToString("HH:mm:ss")} ure";
                        }
                        catch (Exception e)
                        {
                            match = null;
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException("type", type,
                        "Supported values: Daily, Weekly, Monthly, Quarterly, Semeterly, Yearly and None.");
            }

            if ((match != null) && (match.Success))
            {
                Type = type;
            }
            else
            {
                Type = null;
                PeriodTranslation = PeriodInfo;
            }
        }

        public static string ToInfo(DateTime periodFinalDate, char recurrencyType)
        {
            ReportingPeriodType type = ReportingPeriodTypeExtensions.From(recurrencyType);
            return ToInfo(periodFinalDate, type);
        }

        public static string ToInfo(DateTime periodFinalDate, ReportingPeriodType type)
        {
            switch (type)
            {
                case ReportingPeriodType.Daily:
                    return periodFinalDate.ToString("yyyy-MM-dd");

                case ReportingPeriodType.Yearly:
                    return periodFinalDate.ToString("yyyy");

                case ReportingPeriodType.HalfYearly:
                    DateTime halfDate = new DateTime(periodFinalDate.Year, 7, 1);
                    if (periodFinalDate < halfDate)
                        return periodFinalDate.ToString("yyyy") + "-1";
                    return periodFinalDate.ToString("yyyy") + "-2";

                case ReportingPeriodType.Quarterly:
                    DateTime quarter2Date = new DateTime(periodFinalDate.Year, 4, 1);
                    DateTime quarter3Date = new DateTime(periodFinalDate.Year, 7, 1);
                    DateTime quarter4Date = new DateTime(periodFinalDate.Year, 10, 1);
                    int quarter = 4;
                    if (periodFinalDate < quarter2Date)
                        quarter = 1;
                    else if (periodFinalDate < quarter3Date)
                        quarter = 2;
                    else if (periodFinalDate < quarter4Date)
                        quarter = 3;
                    return String.Format("{0}-{1}", periodFinalDate.Year, quarter);

                case ReportingPeriodType.Monthly:

                    return String.Format("{0}-{1,00}", periodFinalDate.Year, periodFinalDate.Month);

                case ReportingPeriodType.Weekly:
                    int week = 1;
                    DateTime nextWeekDate = FirstDateOfWeekISO8601(periodFinalDate.Year, 2);
                    while (periodFinalDate > nextWeekDate)
                    {
                        week++;
                        nextWeekDate = FirstDateOfWeekISO8601(periodFinalDate.Year, week + 1);
                    }

                    return String.Format("{0}-{1,00}", periodFinalDate.Year, week);

                default: return String.Empty;
            }
        }
    }

    public enum ReportingPeriodType
    {
        Daily = 1,
        Weekly = 2,
        Monthly = 3,
        Quarterly = 4,
        HalfYearly = 5,
        Yearly = 6,
        None = 0
    }

    public static class ReportingPeriodTypeExtensions
    {
        public static ReportingPeriodType From(char type)
        {
            switch (type)
            {
                case 'D':
                    return ReportingPeriodType.Daily;
                case 'W':
                    return ReportingPeriodType.Weekly;
                case 'M':
                    return ReportingPeriodType.Monthly;
                case 'Q':
                    return ReportingPeriodType.Quarterly;
                case 'S':
                    return ReportingPeriodType.HalfYearly;
                case 'Y':
                    return ReportingPeriodType.Yearly;
                case 'X':
                    return ReportingPeriodType.None;
                default:
                    throw new ArgumentOutOfRangeException("ReportingPeriodType From Char " + type);
            }
        }

        public static char ToChar(this ReportingPeriodType type)
        {
            switch (type)
            {
                case ReportingPeriodType.Daily:
                    return 'D';
                case ReportingPeriodType.Weekly:
                    return 'W';
                case ReportingPeriodType.Monthly:
                    return 'M';
                case ReportingPeriodType.Quarterly:
                    return 'Q';
                case ReportingPeriodType.HalfYearly:
                    return 'S';
                case ReportingPeriodType.Yearly:
                    return 'Y';
                case ReportingPeriodType.None:
                    return 'X';
                default:
                    throw new ArgumentOutOfRangeException("ReportingPeriodType", type, null);
            }
        }
    }
}