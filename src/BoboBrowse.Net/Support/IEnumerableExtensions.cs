// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    // TODO: need to search for every place this was used in 3.1.0 and put it back in
    public static class IEnumerableExtensions
    {
        public static T Get<T>(this IEnumerable<T> enumerable, int index)
        {
            if (index < enumerable.Count())
                return enumerable.ElementAt(index);
            else
                return default(T);
        }
    }
}
