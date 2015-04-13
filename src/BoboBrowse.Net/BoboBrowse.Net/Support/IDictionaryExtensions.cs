// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Support
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public static class IDictionaryExtensions
    {
        public static void Put<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (!dictionary.ContainsKey(key))
                dictionary.Add(key, value);
            else
                dictionary[key] = value;
        }

        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            if (key == null) 
                return default(TValue);
            TValue value;
            if (dictionary.TryGetValue(key, out value))
                return value;
            else
                return default(TValue);
        }

        public static void PutAll<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IDictionary<TKey, TValue> values)
        {
            foreach (var pair in values)
            {
                Put(dictionary, pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Converts a dictionary of string, Type to a display string by calling ToString() on the inner type.
        /// Note that this won't work if TValue is an IEnumerable or IDictionary.
        /// </summary>
        /// <typeparam name="TValue">The type of object in dictionary that overrides ToString().</typeparam>
        /// <param name="dictionary">A dictionary with a string key.</param>
        /// <returns>A string suitable for display or debugging.</returns>
        public static string ToDisplayString<TValue>(this IDictionary<string, TValue> dictionary)
        {
            if (dictionary == null)
            {
                return string.Empty;
            }

            return "{" + string.Join(", ", dictionary.Select(kvp => string.Format("{0}={1}", kvp.Key, kvp.Value.ToString())).ToArray()) + "}";
        }
    }
}
