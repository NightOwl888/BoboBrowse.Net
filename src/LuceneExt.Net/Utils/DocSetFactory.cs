
namespace LuceneExt.Utils
{
    using System;

    /// <summary> * Utility class to make appropriate measurement calls to recognize optimal
    /// * representation for an ordered document set based on hints provided and 
    /// * min/max/count values on the docset if available. 
    /// * 
    /// * 
    /// * @author abhasin </summary>
    public class DocSetFactory
    {
        private enum Act
        {
            Min,
            Max,
            Count
        }

        private const int INT_SIZE = 32;
        private const int LONG_SHIFT = 6;
        private const int BITSET_COMP_SWAP_RATIO = 15;
        private static int DEFAULT_MIN = 0;
        private static int DEFAULT_MAX = 3000000;
        private static int DEFAULT_COUNT = 1000;
        private static long DEFAULT_INVOKE = 10000L;
        private static long INVOKE = DEFAULT_INVOKE;
        private static long INT_ARRAY_MAX = 500000;

        public enum FOCUS
        {
            PERFORMANCE,
            SPACE,
            OPTIMAL
        }

        public static DocSet GetDocSetInstance(int min, int max, int count, FOCUS hint)
        {
            // Default to Medians
            if (min == -1 || max == -1 || count == -1)
            {
                min = DEFAULT_MIN;
                max = DEFAULT_MAX;
                count = DEFAULT_COUNT;
            }
            else
            {
                Bucket(min, Act.Min);
                Bucket(max, Act.Max);
                Bucket(count, Act.Count);
            }

            INVOKE++;
            if (INVOKE == long.MaxValue)
                INVOKE = 10000L;

            switch (hint)
            {
                // Always Favor IntArray or OpenBitSet
                case FOCUS.PERFORMANCE:
                    if ((((max - min) >> LONG_SHIFT) + 1) * 2 * INT_SIZE > count * INT_SIZE)
                    {
                        return new IntArrayDocIdSet(count);
                    }
                    else
                    {
                        //return new IntArrayDocIdSet(count);
                        return new OBSDocIdSet(max - min + 1);
                    }

                // Always Favor BitSet or Compression   
                case FOCUS.SPACE:
                    if ((max - min) / count < BITSET_COMP_SWAP_RATIO)
                    {
                        return new OBSDocIdSet(max - min + 1);
                    }
                    else
                    {
                        return new P4DDocIdSet();
                    }

                // All cases in consideration  
                case FOCUS.OPTIMAL:
                    if ((max - min) / count > BITSET_COMP_SWAP_RATIO)
                    {
                        if (count < AbstractDocSet.DEFAULT_BATCH_SIZE)
                        {
                            return new IntArrayDocIdSet(count);
                        }
                        else
                        {
                            return new P4DDocIdSet();
                        }
                    }
                    else if ((((max - min) >> LONG_SHIFT) + 1) * 2 * INT_SIZE > count * INT_SIZE)
                    {
                        return new IntArrayDocIdSet(count);
                    }
                    else
                    {
                        return new OBSDocIdSet(max - min + 1);
                    }
            }

            return new IntArrayDocIdSet(count);
        }

        private static void Bucket(int val, Act act)
        {
            switch (act)
            {
                case Act.Min:
                    {
                        DEFAULT_MIN = (int)((DEFAULT_MIN * INVOKE + val) / (INVOKE + 1));
                        break;
                    }

                case Act.Max:
                    {
                        DEFAULT_MAX = (int)((DEFAULT_MAX * INVOKE + val) / (INVOKE + 1));
                        break;
                    }
                case Act.Count:
                    {
                        DEFAULT_COUNT = (int)((DEFAULT_COUNT * INVOKE + val) / (INVOKE + 1));
                        break;
                    }
            }
        }
    }
}
