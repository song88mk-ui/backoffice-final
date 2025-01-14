using BlazorDateRangePicker;

namespace Backoffice.Web.Helpers
{
    public static class DateTimeHelper
    {
        public static DateTime Now(TimeSpan timeZoneOffset)
        {
            var serverOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);

            return DateTime.Now.Add(timeZoneOffset - serverOffset);
        }
        public static DateRange ConvertToClientTimeZone(DateRange range, TimeSpan timeZoneOffset)
        {
            var serverOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);

            TimeSpan timeZoneDifference = serverOffset - timeZoneOffset;

            DateTime clientStartDate = range.Start.Add(timeZoneDifference).DateTime;
            DateTime clientEndDate = range.End.Add(timeZoneDifference).DateTime;

            return new DateRange
            {
                Start = clientStartDate,
                End = clientEndDate
            };
        }
    }
}
