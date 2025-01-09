namespace Backoffice.Web.Helpers
{
    public static class DateTimeHelper
    {
        public static DateTime Now(TimeSpan timeZoneOffset)
        {
            var serverOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);

            return DateTime.Now.Add(timeZoneOffset - serverOffset);
        }
    }
}
