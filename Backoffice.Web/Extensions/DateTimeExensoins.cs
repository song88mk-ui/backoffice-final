using Backoffice.Web.States;

namespace Backoffice.Web.Extensions
{
    public static class DateTimeExensoins
    {
        public static DateTime ToUniversalTime(this DateTime dateTime, TimeSpan timeSpan) => dateTime.Subtract(timeSpan);
    }
}
