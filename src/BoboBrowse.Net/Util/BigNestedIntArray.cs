// Copyright (c) COMPANY. All rights reserved. 

namespace BoboBrowse.Net.Util
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Query.Scoring;
    using Lucene.Net.Util;
    using System;

    /// <summary> * write-once big nested int array
    /// * @author ymatsuda
    /// * </summary>
    public sealed class BigNestedIntArray
    {
        public const int MAX_ITEMS = 1024;
        private const int MAX_SLOTS = 1024;
        private const int SLOTID_MASK = 0x3FF;
        private const int PAGEID_SHIFT = 10;
        private const int COUNT_MASK = 0x7FF;
        private const int VALIDX_SHIFT = 11;
        private const int ROUNDING = 255;

        private const int MISSING = int.MinValue;

        private int _maxItems = MAX_ITEMS;
        private int[][] _list;
        private int _size;

        private static readonly string[] EMPTY = new string[0];

        public abstract class Loader
        {
            private int[][] _list;
            private int _curPageNo;
            private int[] _curPage;
            private int _curSlot;
            private int _curData;

            private int[][] _reuse;
            private int[] _reuseIdx;
            public int reuseUsage;

            private static readonly Comparison<int[]> COMPARE_ARRAYSIZE =
                delegate(int[] o1, int[] o2)
                {
                    if (o1 == null || o2 == null)
                    {
                        if (o1 != null)
                            return -1;
                        if (o2 != null)
                            return 1;
                        return 0;
                    }
                    return (o1.Length - o2.Length);
                };

            private void reclaim(int[][] list)
            {
                _reuse = null;
                _reuseIdx = null;
                reuseUsage = 0;

                if (list != null && list.Length > 0)
                {
                    System.Array.Sort(list, COMPARE_ARRAYSIZE); // sort by size
                    for (int i = (list.Length - 1); i >= 0; i--)
                    {
                        if (list[i] != null)
                        {
                            _reuse = list;
                            _reuseIdx = list[i]; // use the largest page for tracking
                            break;
                        }
                    }
                    if (_reuseIdx == null)
                        return;

                    Arrays.Fill(_reuseIdx, -1);
                    for (int i = 0; i < list.Length; i++)
                    {
                        if (list[i] == null)
                            break;

                        int idx = (list[i]).Length - 1;
                        if (idx >= 0 && _reuseIdx[idx] == -1)
                            _reuseIdx[idx] = i;
                    }
                }
            }

            private int[] alloc(int size)
            {
                size += (ROUNDING - 1);
                size -= (size % ROUNDING);

                if (_reuseIdx != null && _reuseIdx.Length >= size)
                {
                    int location = _reuseIdx[size - 1];
                    if (location >= 0 && location < _reuse.Length)
                    {
                        int[] page = _reuse[location];
                        if (page != null && page.Length == size)
                        {
                            // found a reusable page
                            _reuseIdx[size - 1]++;
                            _reuse[location] = null;

                            if (page == _reuseIdx)
                            {
                                // find a replacement page for reuseIdx
                                for (int i = location; i >= 0; i--)
                                {
                                    if (_reuse[i] != null)
                                    {
                                        _reuseIdx = _reuse[i];
                                        System.Array.Copy(page, 0, _reuseIdx, 0, _reuseIdx.Length);
                                    }
                                }
                            }
                            reuseUsage += size;
                            return page;
                        }
                        else
                        {
                            // no more page with this size
                            _reuseIdx[size - 1] = -1;
                        }
                    }
                }
                return new int[size];
            }

            ///     <summary> * initializes the loading context </summary>
            ///     * <param name="size"> </param>
            public void initialize(int size, int[][] oldList)
            {
                reclaim(oldList);

                _list = new int[(size + MAX_SLOTS - 1) / MAX_SLOTS][];
                _curPageNo = 0;
                _curSlot = 0;
                _curData = MAX_SLOTS;
                _curPage = new int[MAX_SLOTS * 2];
            }

            ///     <summary> * finishes loading </summary>
            public int[][] finish()
            {
                if (_list.Length > _curPageNo)
                {
                    // save the last page
                    while (_curSlot < MAX_SLOTS)
                    {
                        _curPage[_curSlot++] = MISSING;
                    }
                    _list[_curPageNo] = copyPageTo(alloc(_curData));
                }
                _reuse = null;
                _reuseIdx = null;

                return _list;
            }

            ///     <summary> * loads data </summary>
            public abstract void Load();// throws Exception;

            ///     <summary> * reserves storage for the next int array data </summary>
            ///     * <param name="id"> </param>
            ///     * <param name="size"> </param>
            protected void reserve(int id, int size)
            {
                int pageNo = (id >> PAGEID_SHIFT);
                int slotId = (id & SLOTID_MASK);

                if (pageNo != _curPageNo)
                {
                    if (pageNo < _curPageNo)
                        throw new System.ArgumentException("id is out of order");

                    // save the current page

                    while (_curSlot < MAX_SLOTS)
                    {
                        _curPage[_curSlot++] = MISSING;
                    }
                    _list[_curPageNo++] = copyPageTo(alloc(_curData));

                    _curSlot = 0;
                    _curData = MAX_SLOTS;

                    while (_curPageNo < pageNo)
                    {
                        _list[_curPageNo++] = null;
                    }
                }
                else
                {
                    if (_curPageNo == pageNo && _curSlot > slotId)
                        throw new System.ArgumentException("id is out of order");
                }

                while (_curSlot < slotId)
                {
                    _curPage[_curSlot++] = MISSING;
                }

                if (_curPage.Length <= _curData + size)
                {
                    // double the size of the variable part at least
                    _curPage = copyPageTo(new int[_curPage.Length + Math.Max((_curPage.Length - MAX_SLOTS), size)]);
                }
            }

            ///     <summary> * stores int array data. must call reserve(int,int) first to allocate storage  </summary>
            ///     * <param name="data"> </param>
            ///     * <param name="off"> </param>
            ///     * <param name="len"> </param>
            protected void store(int[] data, int off, int len)
            {
                if (len == 0)
                {
                    _curPage[_curSlot] = MISSING;
                }
                else if (len == 1 && data[off] >= 0)
                {
                    _curPage[_curSlot] = data[off];
                }
                else
                {
                    _curPage[_curSlot] = ((-_curData) << VALIDX_SHIFT | len);
                    System.Array.Copy(data, off, _curPage, _curData, len);
                    _curData += len;
                }
                _curSlot++;
            }

            protected void Add(int id, int[] data, int off, int len)
            {
                reserve(id, len);
                store(data, off, len);
            }

            ///     <summary> * allocates storage for future calls of setData. </summary>
            ///     * <param name="id"> </param>
            ///     * <param name="len"> </param>
            protected void allocate(int id, int len, bool nonNegativeIntOnly)
            {
                reserve(id, len);
                if (len == 0)
                {
                    _curPage[_curSlot] = MISSING;
                }
                else if (len == 1 && nonNegativeIntOnly)
                {
                    _curPage[_curSlot] = 0;
                }
                else
                {
                    _curPage[_curSlot] = ((-_curData) << VALIDX_SHIFT);
                    _curData += len;
                }
                _curSlot++;
            }

            protected int[] copyPageTo(int[] dst)
            {
                System.Array.Copy(_curPage, 0, dst, 0, _curData);
                return dst;
            }
        }

        ///   <summary> * Constructs BigNEstedIntArray </summary>
        ///   * <exception cref="Exception"> </exception>
        public BigNestedIntArray()
        {
        }

        ///   <summary> * set maximum number of items per doc. </summary>
        ///   * <param name="maxItems"> </param>
        public void setMaxItems(int maxItems)
        {
            _maxItems = Math.Min(maxItems, MAX_ITEMS);
        }

        ///   <summary> * get maximum number of items per doc. </summary>
        ///   * <returns> maxItems </returns>
        public int getMaxItems()
        {
            return _maxItems;
        }

        ///   <summary> * loads data using the loader </summary>
        ///   * <param name="size"> </param>
        ///   * <param name="loader"> </param>
        ///   * <exception cref="Exception"> </exception>
        public void load(int size, Loader loader) //throws Exception
        {
            _size = size;
            loader.initialize(size, _list);
            if (size > 0)
            {
                loader.Load();
            }
            _list = loader.finish();
        }

        public int size()
        {
            return _size;
        }

        ///   <summary> * gets an int data at [id][idx] </summary>
        ///   * <param name="id"> </param>
        ///   * <param name="idx"> </param>
        ///   * <param name="defaultValue">
        ///   * @return </param>
        public int getData(int id, int idx, int defaultValue)
        {
            int[] page = _list[id >> PAGEID_SHIFT];
            if (page == null)
                return defaultValue;

            int val = page[id & SLOTID_MASK];
            if (val >= 0)
            {
                return val;
            }
            else if (val == MISSING)
            {
                return defaultValue;
            }
            else
            {
                val >>= VALIDX_SHIFT; // signed shift, remember this is a negative number
                return page[idx - val];
            }
        }

        ///     <summary> * gets an int data at [id] </summary>
        ///   * <param name="id"> </param>
        ///   * <param name="buf"> </param>
        ///   * <param name="defaultValue"> </param>
        ///   * <returns> length </returns>
        public int getData(int id, int[] buf)
        {
            int[] page = _list[id >> PAGEID_SHIFT];
            if (page == null)
                return 0;

            int val = page[id & SLOTID_MASK];
            if (val >= 0)
            {
                buf[0] = val;
                return 1;
            }
            else if (val == MISSING)
            {
                return 0;
            }
            else
            {
                int num = (val & COUNT_MASK);
                val >>= VALIDX_SHIFT; // signed shift, remember this is a negative number
                System.Array.Copy(page, (-val), buf, 0, num);
                return num;
            }
        }

        ///   <summary> * translates the int value using the val list </summary>
        ///   * @param <T> </param>
        ///   * <param name="array"> </param>
        ///   * <param name="id"> </param>
        ///   * <param name="valarray">
        ///   * @return </param>
        public string[] getTranslatedData(int id, ITermValueList valarray)
        {
            int[] page = _list[id >> PAGEID_SHIFT];

            if (page == null)
            {
                return EMPTY;
            }
            else
            {
                int val = page[id & SLOTID_MASK];

                if (val >= 0)
                {
                    return new string[] { valarray.Get(val) };
                }
                else if (val == MISSING)
                {
                    return EMPTY;
                }
                else
                {
                    int num = (val & COUNT_MASK);
                    val >>= VALIDX_SHIFT; // signed shift, remember this is a negative number

                    string[] ret = new string[num];
                    for (int i = 0; i < num; i++)
                    {
                        ret[i] = valarray.Get(page[i - val]);
                    }
                    return ret;
                }
            }
        }

        ///   <summary> * translates the int value using the val list </summary>
        ///   * @param <T> </param>
        ///   * <param name="array"> </param>
        ///   * <param name="id"> </param>
        ///   * <param name="valarray">
        ///   * @return </param>
        public object[] getRawData(int id, ITermValueList valarray)
        {
            int[] page = _list[id >> PAGEID_SHIFT];

            if (page == null)
            {
                return EMPTY;
            }
            else
            {
                int val = page[id & SLOTID_MASK];

                if (val >= 0)
                {
                    return new object[] { valarray.GetRawValue(val) };
                }
                else if (val == MISSING)
                {
                    return EMPTY;
                }
                else
                {
                    int num = (val & COUNT_MASK);
                    val >>= VALIDX_SHIFT; // signed shift, remember this is a negative number

                    object[] ret = new object[num];
                    for (int i = 0; i < num; i++)
                    {
                        ret[i] = valarray.GetRawValue(page[i - val]);
                    }
                    return ret;
                }
            }
        }

        public float getScores(int id, int[] freqs, float[] boosts, IFacetTermScoringFunction function)
        {
            function.ClearScores();
            int[] page = _list[id >> PAGEID_SHIFT];
            int val = page[id & SLOTID_MASK];

            if (val >= 0)
            {
                return function.Score(freqs[val], boosts[val]);
            }
            else
            {
                int num = (val & COUNT_MASK);
                val >>= VALIDX_SHIFT; // signed shift, remember this is a negative number
                int idx;
                for (int i = 0; i < num; i++)
                {
                    idx = page[i - val];
                    function.ScoreAndCollect(freqs[idx], boosts[idx]);
                }
                return function.GetCurrentScore();
            }
        }

        public int compare(int i, int j)
        {
            int[] page1 = _list[i >> PAGEID_SHIFT];
            int[] page2 = _list[j >> PAGEID_SHIFT];

            if (page1 == null)
            {
                if (page2 == null)
                    return 0;
                else
                    return -1;
            }
            else
            {
                if (page2 == null)
                    return 1;
            }

            int val1 = page1[i & SLOTID_MASK];
            int val2 = page2[j & SLOTID_MASK];

            if (val1 >= 0 && val2 >= 0)
                return val1 - val2;

            if (val1 >= 0)
            {
                if (val2 == MISSING)
                    return 1;
                int idx = -(val2 >> VALIDX_SHIFT); // signed shift, remember this is a negative number
                int val = val1 - page2[idx];
                if (val == 0)
                {
                    return -1;
                }
                else
                {
                    return val;
                }
            }
            if (val2 >= 0)
            {
                if (val1 == MISSING)
                    return -1;
                int idx = -(val1 >> VALIDX_SHIFT); // signed shift, remember this is a negative number
                int val = page1[idx] - val2;
                if (val == 0)
                {
                    return 1;
                }
                else
                {
                    return val;
                }
            }

            if (val1 == MISSING)
            {
                if (val2 == MISSING)
                {
                    return 0;
                }
                else
                    return -1;
            }
            else
            {
                if (val2 == MISSING)
                {
                    return 1;
                }
            }

            int idx1 = -(val1 >> VALIDX_SHIFT); // signed shift, remember this is a negative number
            int len1 = (val1 & COUNT_MASK);

            int idx2 = -(val2 >> VALIDX_SHIFT); // signed shift, remember this is a negative number
            int len2 = (val2 & COUNT_MASK);

            for (int k = 0; k < len1; ++k)
            {
                if (k >= len2)
                {
                    return 1;
                }

                int compVal = page1[idx1 + k] - page2[idx2 + k];
                if (compVal != 0)
                    return compVal;
            }
            if (len1 == len2)
                return 0;
            return -1;
        }

        public bool Contains(int id, int value)
        {
            int[] page = _list[id >> PAGEID_SHIFT];
            if (page == null)
                return false;

            int val = page[id & SLOTID_MASK];
            if (val >= 0)
            {
                return (val == value);
            }
            else if (val != MISSING)
            {
                int idx = -(val >> VALIDX_SHIFT); // signed shift, remember this is a negative number
                int end = idx + (val & COUNT_MASK);
                while (idx < end)
                {
                    if (page[idx++] == value)
                        return true;
                }
            }
            return false;
        }

        public bool Contains(int id, OpenBitSet values)
        {
            int[] page = _list[id >> PAGEID_SHIFT];
            if (page == null)
                return false;

            int val = page[id & SLOTID_MASK];
            if (val >= 0)
            {
                return (values.FastGet(val));
            }
            else if (val != MISSING)
            {
                int idx = -(val >> VALIDX_SHIFT); // signed shift, remember this is a negative number
                int end = idx + (val & COUNT_MASK);
                while (idx < end)
                {
                    if (values.FastGet(page[idx++]))
                        return true;
                }
            }
            return false;
        }

        public int count(int id, int[] count)
        {
            int[] page = _list[id >> PAGEID_SHIFT];
            if (page == null)
            {
                count[0]++;
                return 0;
            }

            int val = page[id & SLOTID_MASK];
            if (val >= 0)
            {
                count[val]++;
                return 1;
            }
            else if (val != MISSING)
            {
                int idx = -(val >> VALIDX_SHIFT); // signed shift, remember val is a negative number
                int cnt = (val & COUNT_MASK);
                int end = idx + cnt;
                while (idx < end)
                {
                    count[page[idx++]]++;
                }
                return cnt;
            }
            count[0]++;
            return 0;
        }

        ///   <summary> * returns the number data items for id </summary>
        ///   * <param name="id">
        ///   * @return </param>
        public int getNumItems(int id)
        {
            int[] page = _list[id >> PAGEID_SHIFT];
            if (page == null)
                return 0;

            int val = page[id & SLOTID_MASK];

            if (val >= 0)
                return 1;

            if (val == MISSING)
                return 0;

            return (val & COUNT_MASK);
        }

        ///   <summary> * adds Data to id </summary>
        public bool addData(int id, int data)
        {
            int[] page = _list[id >> PAGEID_SHIFT];
            if (page == null)
                return true;

            int slotId = (id & SLOTID_MASK);
            int val = page[slotId];

            if (val == MISSING)
            {
                return true; // don't store
            }
            else if (val >= 0)
            {
                page[slotId] = data; // only one value
                return true;
            }
            else
            {
                int num = (val & COUNT_MASK);
                if (num >= _maxItems)
                    return false;

                val >>= VALIDX_SHIFT; // signed shift, remember this is a negative number
                page[num - val] = data;
                val = ((val << VALIDX_SHIFT) | (num + 1));
                page[slotId] = val;
                return true;
            }
        }

        ///   <summary> * A loader that buffer all data in memory, then load them to BigNestedIntArray.
        ///   * Data does not need to be sorted prior to the operation.
        ///   * Note that this loader supports only non-negative integer data. </summary>
        public class BufferedLoader : Loader
        {
            private static int EOD = int.MinValue;
            private static int SEGSIZE = 8;

            private int _size;
            private BigIntArray _info;
            private BigIntBuffer _buffer;
            private int _maxItems;

            public BufferedLoader(int size, int maxItems, BigIntBuffer buffer)
            {
                _size = size;
                _maxItems = Math.Min(maxItems, BigNestedIntArray.MAX_ITEMS);
                _info = new BigIntArray(size << 1); // pointer and count
                _info.Fill(EOD);
                _buffer = buffer;
            }

            public BufferedLoader(int size)
                : this(size, MAX_ITEMS, new BigIntBuffer())
            {
            }

            ///     <summary> * resets loader. This also resets underlying BigIntBuffer. </summary>
            public void reset(int size, int maxItems, BigIntBuffer buffer)
            {
                if (size >= capacity())
                    throw new System.ArgumentException("unable to change size");
                _size = size;
                _maxItems = maxItems;
                _info.Fill(EOD);
                _buffer = buffer;
            }

            ///     <summary> * adds a pair of id and value to the buffer </summary>
            ///     * <param name="id"> </param>
            ///     * <param name="val"> </param>
            public bool Add(int id, int val)
            {
                int ptr = _info.Get(id << 1);
                if (ptr == EOD)
                {
                    // 1st insert
                    _info.Add(id << 1, val);
                    return true;
                }

                int cnt = _info.Get((id << 1) + 1);
                if (cnt == EOD)
                {
                    // 2nd insert
                    _info.Add((id << 1) + 1, val);
                    return true;
                }

                if (ptr >= 0)
                {
                    // this id has two values stored in-line.
                    int firstVal = ptr;
                    int secondVal = cnt;

                    ptr = _buffer.Alloc(SEGSIZE);
                    _buffer.Set(ptr++, EOD);
                    _buffer.Set(ptr++, firstVal);
                    _buffer.Set(ptr++, secondVal);
                    _buffer.Set(ptr++, val);
                    cnt = 3;
                }
                else
                {
                    ptr = (-ptr);
                    if (cnt >= _maxItems) // exceeded the limit
                        return false;

                    if ((ptr % SEGSIZE) == 0)
                    {
                        int oldPtr = ptr;
                        ptr = _buffer.Alloc(SEGSIZE);
                        _buffer.Set(ptr++, (-oldPtr));
                    }
                    _buffer.Set(ptr++, val);
                    cnt++;
                }

                _info.Add(id << 1, (-ptr));
                _info.Add((id << 1) + 1, cnt);

                return true;
            }

            private int readToBuf(int id, int[] buf)
            {
                int ptr = _info.Get(id << 1);
                int cnt = _info.Get((id << 1) + 1);
                int i;

                if (ptr >= 0)
                {
                    // read in-line data
                    i = 0;
                    buf[i++] = ptr;
                    if (cnt >= 0)
                        buf[i++] = cnt;
                    return i;
                }

                // read from segments
                i = cnt;
                while (ptr != EOD)
                {
                    ptr = (-ptr) - 1;
                    int val;
                    while ((val = _buffer.Get(ptr--)) >= 0)
                    {
                        buf[--i] = val;
                    }
                    ptr = val;
                }
                if (i > 0)
                {
                    throw new RuntimeException("error reading buffered data back");
                }

                return cnt;
            }

            public override void Load()
            {
                int[] buf = new int[MAX_ITEMS];
                int size = _size;
                for (int i = 0; i < size; i++)
                {
                    int count = readToBuf(i, buf);
                    if (count > 0)
                    {
                        Add(i, buf, 0, count);
                    }
                }
            }

            public int capacity()
            {
                return _info.Capacity() >> 1;
            }
        }
    }
}
