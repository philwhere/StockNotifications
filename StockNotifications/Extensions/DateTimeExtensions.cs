using System;

namespace StockNotifications.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// yyyy-MM-dd
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string ToTableDateFormat(this DateTime dateTime) => $"{dateTime:yyyy-MM-dd}";
    }
}
