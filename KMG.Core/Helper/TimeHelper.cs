namespace KMG.Core.Helper
{
    public static class TimeHelper
    {
        private static readonly TimeZoneInfo EgyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");

        public static DateTime NowInEgypt
        {
            get
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, EgyptTimeZone);
            }
        }
    }
}
