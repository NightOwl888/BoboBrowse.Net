namespace BoboBrowse.Net.Support
{
    using System;

    /// <summary>
    /// A TimeUnit represents time durations at a given unit of
    /// granularity and provides utility methods to convert across units,
    /// and to perform timing and delay operations in these units.  A
    /// TimeUnit does not maintain time information, but only
    /// helps organize and use time representations that may be maintained
    /// separately across various contexts.  A nanosecond is defined as one
    /// thousandth of a microsecond, a microsecond as one thousandth of a
    /// millisecond, a millisecond as one thousandth of a second, a minute
    /// as sixty seconds, an hour as sixty minutes, and a day as twenty four
    /// hours.
    /// 
    /// A TimeUnit is mainly used to inform time-based methods
    /// how a given timing parameter should be interpreted. For example,
    /// the following code will timeout in 50 milliseconds if the
    /// lock is not available:
    /// 
    /// <pre>  Lock lock = ...; 
    /// if ( lock.tryLock(50L, TimeUnit.MILLISECONDS) ) ...
    /// </pre>
    /// while this code will timeout in 50 seconds:
    /// <pre>
    /// Lock lock = ...;
    /// if ( lock.tryLock(50L, TimeUnit.SECONDS) ) ...
    /// </pre>
    /// 
    /// Note however, that there is no guarantee that a particular timeout
    /// implementation will be able to notice the passage of time at the
    /// same granularity as the given TimeUnit.
    /// </summary>
    /// <remarks>
    /// Source: http://fuseyism.com/classpath/doc/java/util/concurrent/TimeUnit-source.html
    /// </remarks>
    public enum TimeUnit
    {
        NANOSECONDS,
        MICROSECONDS,
        MILLISECONDS,
        SECONDS,
        MINUTES,
        HOURS,
        DAYS
    }

    /// <summary>
    /// Extension methods to make the TimeUnit enumeration act like it does in Java.
    /// </summary>
    public static class TimeUnitExtensions
    {
        // Handy constants for conversion methods
        static const long C0 = 1L;
        static const long C1 = C0 * 1000L;
        static const long C2 = C1 * 1000L;
        static const long C3 = C2 * 1000L;
        static const long C4 = C3 * 60L;
        static const long C5 = C4 * 60L;
        static const long C6 = C5 * 24L;

        static const long MAX = long.MaxValue;

        public static long ToNanos(this TimeUnit timeUnit, long d)
        {
            switch (timeUnit)
            {
                case TimeUnit.NANOSECONDS:
                    return d;
                case TimeUnit.MICROSECONDS:
                    return x(d, C1 / C0, MAX / (C1 / C0));
                case TimeUnit.MILLISECONDS:
                    return x(d, C2 / C0, MAX / (C2 / C0));
                case TimeUnit.SECONDS:
                    return x(d, C3 / C0, MAX / (C3 / C0));
                case TimeUnit.MINUTES:
                    return x(d, C4 / C0, MAX / (C4 / C0)); 
                case TimeUnit.HOURS:
                    return x(d, C5 / C0, MAX / (C5 / C0));
                case TimeUnit.DAYS:
                    return x(d, C6 / C0, MAX / (C6 / C0));
            }
            return d; // Default to TimeUnit.NANOSECONDS
        }

        public static long ToMicros(this TimeUnit timeUnit, long d)
        {
            switch (timeUnit)
            {
                case TimeUnit.NANOSECONDS:
                    return d / (C1 / C0);
                case TimeUnit.MICROSECONDS:
                    return d;
                case TimeUnit.MILLISECONDS:
                    return x(d, C2 / C1, MAX / (C2 / C1));
                case TimeUnit.SECONDS:
                    return x(d, C3 / C1, MAX / (C3 / C1));
                case TimeUnit.MINUTES:
                    return x(d, C4 / C1, MAX / (C4 / C1));
                case TimeUnit.HOURS:
                    return x(d, C5 / C1, MAX / (C5 / C1));
                case TimeUnit.DAYS:
                    return x(d, C6 / C1, MAX / (C6 / C1));
            }
            return d; // Default to TimeUnit.MICROSECONDS
        }

        public static long ToMillis(this TimeUnit timeUnit, long d)
        {
            switch (timeUnit)
            {
                case TimeUnit.NANOSECONDS:
                    return d / (C2 / C0);
                case TimeUnit.MICROSECONDS:
                    return d / (C2 / C1);
                case TimeUnit.MILLISECONDS:
                    return d;
                case TimeUnit.SECONDS:
                    return x(d, C3 / C2, MAX / (C3 / C2));
                case TimeUnit.MINUTES:
                    return x(d, C4 / C2, MAX / (C4 / C2));
                case TimeUnit.HOURS:
                    return x(d, C5 / C2, MAX / (C5 / C2));
                case TimeUnit.DAYS:
                    return x(d, C6 / C2, MAX / (C6 / C2));
            }
            return d; // Default to TimeUnit.MILLISECONDS
        }

        public static long ToSeconds(this TimeUnit timeUnit, long d)
        {
            switch (timeUnit)
            {
                case TimeUnit.NANOSECONDS:
                    return d / (C3 / C0);
                case TimeUnit.MICROSECONDS:
                    return d / (C3 / C1);
                case TimeUnit.MILLISECONDS:
                    return d / (C3 / C2);
                case TimeUnit.SECONDS:
                    return d; 
                case TimeUnit.MINUTES:
                    return x(d, C4 / C3, MAX / (C4 / C3));
                case TimeUnit.HOURS:
                    return x(d, C5 / C3, MAX / (C5 / C3));
                case TimeUnit.DAYS:
                    return x(d, C6 / C3, MAX / (C6 / C3));
            }
            return d; // Default to TimeUnit.SECONDS
        }

        public static long ToMinutes(this TimeUnit timeUnit, long d)
        {
            switch (timeUnit)
            {
                case TimeUnit.NANOSECONDS:
                    return d / (C4 / C0);
                case TimeUnit.MICROSECONDS:
                    return d / (C4 / C1);
                case TimeUnit.MILLISECONDS:
                    return d / (C4 / C2);
                case TimeUnit.SECONDS:
                    return d / (C4 / C3);
                case TimeUnit.MINUTES:
                    return d;
                case TimeUnit.HOURS:
                    return x(d, C5 / C4, MAX / (C5 / C4));
                case TimeUnit.DAYS:
                    return x(d, C6 / C4, MAX / (C6 / C4));
            }
            return d; // Default to TimeUnit.MINUTES
        }

        public static long ToHours(this TimeUnit timeUnit, long d)
        {
            switch (timeUnit)
            {
                case TimeUnit.NANOSECONDS:
                    return d / (C5 / C0);
                case TimeUnit.MICROSECONDS:
                    return d / (C5 / C1);
                case TimeUnit.MILLISECONDS:
                    return d / (C5 / C2);
                case TimeUnit.SECONDS:
                    return d / (C5 / C3);
                case TimeUnit.MINUTES:
                    return d / (C5 / C4);
                case TimeUnit.HOURS:
                    return d;
                case TimeUnit.DAYS:
                    return x(d, C6 / C5, MAX / (C6 / C5));
            }
            return d; // Default to TimeUnit.HOURS
        }

        public static long ToDays(this TimeUnit timeUnit, long d)
        {
            switch (timeUnit)
            {
                case TimeUnit.NANOSECONDS:
                    return d / (C6 / C0);
                case TimeUnit.MICROSECONDS:
                    return d / (C6 / C1);
                case TimeUnit.MILLISECONDS:
                    return d / (C6 / C2);
                case TimeUnit.SECONDS:
                    return d / (C6 / C3);
                case TimeUnit.MINUTES:
                    return d / (C6 / C4);
                case TimeUnit.HOURS:
                    return d / (C6 / C5);
                case TimeUnit.DAYS:
                    return d;
            }
            return d; // Default to TimeUnit.DAYS
        }

        public static long Convert(this TimeUnit timeUnit, long d, TimeUnit u)
        {
            switch (timeUnit)
            {
                case TimeUnit.NANOSECONDS:
                    return u.ToNanos(d);
                case TimeUnit.MICROSECONDS:
                    return u.ToMicros(d);
                case TimeUnit.MILLISECONDS:
                    return u.ToMillis(d);
                case TimeUnit.SECONDS:
                    return u.ToSeconds(d);
                case TimeUnit.MINUTES:
                    return u.ToMinutes(d);
                case TimeUnit.HOURS:
                    return u.ToHours(d);
                case TimeUnit.DAYS:
                    return u.ToDays(d);
            }
            return d;
        }
        
        static long x(long d, long m, long over) 
        {
            if (d >  over) return long.MaxValue;
            if (d < -over) return long.MinValue;
            return d * m;
        }
    }
}
