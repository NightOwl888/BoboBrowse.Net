// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Support
{
    using System.Collections.Generic;
    using System.Linq;

    public static class IEnumerableExtensions
    {
        public static T Get<T>(this IEnumerable<T> enumerable, int index) where T : class
        {
            if (index < enumerable.Count())
                return enumerable.ElementAt(index);
            else
                return default(T);
        }

        public static T Get<T>(this IEnumerable<T> enumerable, int index, T defaultValue)
        {
            if (index < enumerable.Count())
                return enumerable.ElementAt(index);
            else
                return defaultValue;
        }
    }
}
