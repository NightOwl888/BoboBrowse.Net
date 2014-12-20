using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoboBrowse.Net.Support
{
    internal static class DateTimeExtensions
    {
        /// <summary>
        /// Returns the number of milliseconds since January 1, 1970, 00:00:00 GMT represented by this DateTime object.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static long GetTime(this DateTime date)
        {
            DateTime startDate = new DateTime(1970, 1, 1);
            return (long)date.Subtract(startDate).TotalMilliseconds;
        }
    }
}
