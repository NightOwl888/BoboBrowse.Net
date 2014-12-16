namespace BoboBrowse.Net.Support
{
    using System;
    using System.Linq;

    internal static class Arrays
    {
        public static void Fill<T>(T[] block, T value)
        {
            for (int i = 0; i < block.Length; i++)
            {
                block[i] = value;
            }
        }

        public static string ToString<T>(T[] block)
        {
            return string.Join(", ", block.Select(x => x.ToString()).ToArray());
        }

        public static int HashCode(long[] array)
        {
            if (array == null)
            {
                return 0;
            }
            int hashCode = 1;
            foreach (long elementValue in array)
            {
                /*
                 * the hash code value for long value is (int) (value ^ (value >>>
                 * 32))
                 */
                hashCode = 31 * hashCode
                        + (int)(elementValue ^ (long)(((ulong)elementValue) >> 32));
            }
            return hashCode;
        }
    }
}
