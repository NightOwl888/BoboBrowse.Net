﻿// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Data
{
    using BoboBrowse.Net.Support;
    using System;

    public class PrimitiveLongArrayWrapper
    {
        public long[] data;

        public PrimitiveLongArrayWrapper(long[] data)
        {
            this.data = data;
        }

        public override bool Equals(object other)
        {
            if (other is PrimitiveLongArrayWrapper)
            {
                return Arrays.Equals(data, ((PrimitiveLongArrayWrapper)other).data);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Arrays.HashCode(data);
        }
    }
}
