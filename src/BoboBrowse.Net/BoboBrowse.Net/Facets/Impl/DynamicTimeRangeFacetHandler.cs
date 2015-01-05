// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Support;
    using Common.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class DynamicTimeRangeFacetHandler : DynamicRangeFacetHandler
    {
        private static ILog log = LogManager.GetLogger<DynamicTimeRangeFacetHandler>();
        public const string NUMBER_FORMAT = "00000000000000000000";

        public const long MILLIS_IN_DAY = 24L * 60L * 60L * 1000L;
        public const long MILLIS_IN_HOUR = 60L * 60L * 1000L;
        public const long MILLIS_IN_MIN = 60L * 1000L;
        public const long MILLIS_IN_SEC = 1000L;

        private readonly IDictionary<string, string> _valueToRangeStringMap;
        private readonly IDictionary<string, string> _rangeStringToValueMap;
        private readonly IList<string> _rangeStringList;

        /// <summary>
        /// Initializes a new instance of <see cref="T:DynamicTimeRangeFacetHandler"/>.
        /// The format of range string is dddhhmmss. (ddd: days (000-999), hh : hours (00-23), mm: minutes (00-59), ss: seconds (00-59))
        /// </summary>
        /// <param name="name">The facet handler name.</param>
        /// <param name="dataFacetName">The facet handler this one depends on.</param>
        /// <param name="currentTime">The number of milliseconds since January 1, 1970 expessed in universal coordinated time (UTC). 
        /// The <see cref="M:BoboBrowse.Net.Support.DateTimeExtensions.GetTime"/> method can be used to convert the current time to 
        /// this format, e.g. DateTime.Now.GetTime().</param>
        /// <param name="ranges">A list of range strings in the format dddhhmmss. (ddd: days (000-999), hh : hours (00-23), mm: minutes (00-59), ss: seconds (00-59))</param>
        public DynamicTimeRangeFacetHandler(string name, string dataFacetName, long currentTime, IEnumerable<string> ranges)
            : base(name, dataFacetName)
        {
            if (log.IsDebugEnabled)
            {
                log.Debug(name + " " + dataFacetName + " " + currentTime);
            }
            List<string> sortedRanges = new List<string>(ranges);
            sortedRanges.Sort();

            _valueToRangeStringMap = new Dictionary<string, string>();
            _rangeStringToValueMap = new Dictionary<string, string>();
            _rangeStringList = new List<string>(ranges.Count());

            string prev = "000000000";
            foreach (string range in sortedRanges)
            {
                string rangeString = BuildRangeString(currentTime, prev, range);
                _valueToRangeStringMap.Put(range, rangeString);
                _rangeStringToValueMap.Put(rangeString, range);
                _rangeStringList.Add(rangeString);
                prev = range;

                if (log.IsDebugEnabled)
                {
                    log.Debug(range + "\t " + rangeString);
                }
            }
        }

        private DynamicTimeRangeFacetHandler(string name, string dataFacetName,
                                 IDictionary<string, string> valueToRangeStringMap,
                                 IDictionary<string, string> rangeStringToValueMap,
                                 IEnumerable<string> rangeStringList)
            : base(name, dataFacetName)
        {
            
            _valueToRangeStringMap = valueToRangeStringMap;
            _rangeStringToValueMap = rangeStringToValueMap;
            _rangeStringList = new List<string>(rangeStringList);
        }

        private static long GetTime(long time, string range)
        {
            if (range.Length != 9) throw new ParseException("invalid range format: " + range);
            try
            {
                int val;

                val = int.Parse(range.Substring(0, 3));
                time -= val * MILLIS_IN_DAY;

                val = int.Parse(range.Substring(3, 2));
                if (val >= 24) throw new ParseException("invalid range format: " + range);
                time -= val * MILLIS_IN_HOUR;

                val = int.Parse(range.Substring(5, 2));
                if (val >= 60) throw new ParseException("invalid range format: " + range);
                time -= val * MILLIS_IN_MIN;

                val = int.Parse(range.Substring(7, 2));
                if (val >= 60) throw new ParseException("invalid range format: " + range);
                time -= val * MILLIS_IN_SEC;

                return time;
            }
            catch (Exception e)
            {
                throw new ParseException("invalid time format:" + range, e);
            }
        }

        private string BuildRangeString(long currentTime, string dStart, string dEnd)
        {
            // NOTE: The original code used the culture of the current thread for formatting, so that is what we do here.
            string end = GetTime(currentTime, dStart).ToString(NUMBER_FORMAT);
            string start = (GetTime(currentTime, dEnd) + 1).ToString(NUMBER_FORMAT);
            StringBuilder buf = new StringBuilder();
            buf.Append("[").Append(start).Append(" TO ").Append(end).Append("]");
            return buf.ToString();
        }

        protected override string BuildRangeString(string val)
        {
            return _valueToRangeStringMap.Get(val);
        }

        protected override IEnumerable<string> BuildAllRangeStrings()
        {
            return _rangeStringList;
        }

        protected override string GetValueFromRangeString(string val)
        {
            return _rangeStringToValueMap.Get(val);
        }

        public DynamicTimeRangeFacetHandler NewInstance()
        {
            return new DynamicTimeRangeFacetHandler(Name, _dataFacetName, _valueToRangeStringMap, _rangeStringToValueMap, _rangeStringList);
        }
    }
}
