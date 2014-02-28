// Copyright (c) COMPANY. All rights reserved. 

namespace BoboBrowse.Net.Utils
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
    }
}
