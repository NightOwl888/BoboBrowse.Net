namespace BoboBrowse.Net.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public static class IDictionaryExtensions
    {
        public static void Put<TValue>(this IDictionary<string, TValue> dictionary, string key, TValue value)
        {
            if (!dictionary.ContainsKey(key))
                dictionary.Add(key, value);
            else
                dictionary[key] = value;
        }

        public static TValue Get<TValue>(this IDictionary<string, TValue> dictionary, string key)
        {
            TValue value;
            if (dictionary.TryGetValue(key, out value))
                return value;
            else
                return default(TValue);
        }
    }
}
