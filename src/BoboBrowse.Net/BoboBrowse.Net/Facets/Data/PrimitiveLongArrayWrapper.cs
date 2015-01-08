﻿// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Data
{
    using BoboBrowse.Net.Support;

    public class PrimitiveLongArrayWrapper
    {
        private long[] data;

        public PrimitiveLongArrayWrapper(long[] data)
        {
            this.data = data;
        }

        /// <summary>
        /// Added in .NET version as an accessor to the data field.
        /// </summary>
        /// <returns></returns>
        public virtual long[] Data
        {
            get { return data; }
            set { data = value; }
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
