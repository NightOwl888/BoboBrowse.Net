namespace BoboBrowse.Tests
{
    using System;
    using System.Collections;

    public static class BitArrayExtensions
    {
        /// <summary>
        /// Gets the number of bits set in a <see cref="T:System.Collections.BitArray"/>.
        /// </summary>
        /// <remarks>
        /// Source: http://stackoverflow.com/questions/5063178/counting-bits-set-in-a-net-bitarray-class
        /// </remarks>
        /// <param name="bitArray">The BitArray.</param>
        /// <returns>An integer indicating the number of bits set.</returns>
        public static Int32 GetCardinality(this BitArray bitArray)
        {
            Int32[] ints = new Int32[(bitArray.Count >> 5) + 1];

            bitArray.CopyTo(ints, 0);

            Int32 count = 0;

            // fix for not truncated bits in last integer that may have been set to true with SetAll()
            ints[ints.Length - 1] &= ~(-1 << (bitArray.Count % 32));

            for (Int32 i = 0; i < ints.Length; i++)
            {

                Int32 c = ints[i];

                // magic (http://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel)
                unchecked
                {
                    c = c - ((c >> 1) & 0x55555555);
                    c = (c & 0x33333333) + ((c >> 2) & 0x33333333);
                    c = ((c + (c >> 4) & 0xF0F0F0F) * 0x1010101) >> 24;
                }

                count += c;

            }

            return count;
        }
    }
}
