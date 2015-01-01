namespace BoboBrowse.Net.Support
{
    using System.Collections.Generic;

    public static class IListExtensions
    {
        public static T Poll<T>(this IList<T> list) where T : class
        {
            T value = list.Get(0);
            if (list.Count > 0)
                list.RemoveAt(0);
            return value;
        }

        public static T Poll<T>(this IList<T> list, T defaultValue)
        {
            T value = list.Get(0, defaultValue);
            if (list.Count > 0)
                list.RemoveAt(0);
            return value;
        }

        /// <summary>
        /// Removes elements of this type-specific list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="from">the start index (inclusive).</param>
        /// <param name="to">the end index (exclusive).</param>
        public static void RemoveElements<T>(this IList<T> list, int from, int to)
        {
            for (int i = (to - 1); i >= from; i--)
            {
                list.RemoveAt(i);
            }
        }
    }
}
