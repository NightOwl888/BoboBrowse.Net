// Version compatibility level: 3.2.0
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

        /// <summary>
        /// Converts an IEnumerable to a display string by calling the ToString() method of the inner type.
        /// Note that this won't work if the inner type is IEnumerable or IDictionary.
        /// </summary>
        /// <typeparam name="T">The enumerable object type that overrides ToString().</typeparam>
        /// <param name="enumerable">An IEnumerable instance.</param>
        /// <returns>A string suitable for display or debugging.</returns>
        public static string ToDisplayString<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                return string.Empty;
            }

            return "{" + string.Join(", ", enumerable.Select(x => x.ToString()).ToArray()) + "}";
        }
    }
}
