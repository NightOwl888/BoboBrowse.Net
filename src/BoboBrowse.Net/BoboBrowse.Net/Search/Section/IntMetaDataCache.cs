// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Search.Section
{
    using Lucene.Net.Index;
    using System;

    public class IntMetaDataCache : IMetaDataCache
    {
        private static readonly int MAX_SLOTS = 1024;
        private static readonly int MISSING = int.MinValue;

        private readonly IndexReader _reader;
        private int[][] _list;

        private int _curPageNo;
        private int[] _curPage;
        private int _curSlot;
        private int _curData;

        public IntMetaDataCache(Term term, IndexReader reader)
        {
            _reader = reader;

            int maxDoc = reader.MaxDoc;
            _list = new int[(maxDoc + MAX_SLOTS - 1) / MAX_SLOTS][];
            _curPageNo = 0;
            _curSlot = 0;
            _curData = MAX_SLOTS;

            if (maxDoc > 0)
            {
                _curPage = new int[MAX_SLOTS * 2];
                LoadPayload(term);
            }

            _curPage = null;
        }

        protected virtual void Add(int docid, byte[] data, int blen)
        {
            int pageNo = docid / MAX_SLOTS;
            if (pageNo != _curPageNo)
            {
                // save the page

                while (_curSlot < MAX_SLOTS)
                {
                    _curPage[_curSlot++] = MISSING;
                }
                _list[_curPageNo++] = CopyPage(new int[_curData]);  // optimize the page to make getMaxItems work
                _curSlot = 0;
                _curData = MAX_SLOTS;

                while (_curPageNo < pageNo)
                {
                    _list[_curPageNo++] = null;
                }
            }

            while (_curSlot < docid % MAX_SLOTS)
            {
                _curPage[_curSlot++] = MISSING;
            }

            if (blen <= 4)
            {
                int val = 0;
                if (blen == 0)
                {
                    val = MISSING;
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (i >= data.Length) break;

                        val |= ((data[i] & 0xff) << (i * 8));
                    }
                }
                if (val >= 0)
                {
                    _curPage[_curSlot] = val;
                }
                else
                {
                    AppendToTail(data, blen);
                }
            }
            else
            {
                AppendToTail(data, blen);
            }
            _curSlot++;
        }

        private void AppendToTail(byte[] data, int blen)
        {
            int ilen = (blen + 3) / 4; // length in ints

            if (_curPage.Length <= _curData + ilen)
            {
                // double the size of the variable part at least
                _curPage = CopyPage(new int[_curPage.Length + Math.Max((_curPage.Length - MAX_SLOTS), ilen)]);
            }
            _curPage[_curSlot] = (-_curData);
            _curData = CopyByteToInt(data, 0, blen, _curPage, _curData);
        }

        private int CopyByteToInt(byte[] src, int off, int blen, int[] dst, int dstoff)
        {
            while (blen > 0)
            {
                int val = 0;
                for (int i = 0; i < 4; i++)
                {
                    blen--;

                    if (off >= src.Length) break; // may not have all bytes            
                    val |= ((src[off++] & 0xff) << (i * 8));
                }

                dst[dstoff++] = val;
            }
            return dstoff;
        }

        private int[] CopyPage(int[] dst)
        {
            Array.Copy(_curPage, 0, dst, 0, _curData);
            return dst;
        }

        protected virtual void LoadPayload(Term term)
        {
            byte[] payloadBuf = null;
            TermPositions tp = _reader.TermPositions();
            tp.Seek(term);
            while (tp.Next())
            {
                if (tp.Freq > 0)
                {
                    tp.NextPosition();
                    if (tp.IsPayloadAvailable)
                    {
                        int len = tp.PayloadLength;
                        payloadBuf = tp.GetPayload(payloadBuf, 0);
                        Add(tp.Doc, payloadBuf, len);
                    }
                }
            }

            // save the last page

            while (_curSlot < MAX_SLOTS)
            {
                _curPage[_curSlot++] = MISSING;
            }
            _list[_curPageNo] = CopyPage(new int[_curData]); // optimize the page to make getNumItems work
            _curPage = null;
        }

        public virtual int GetValue(int docid, int idx, int defaultValue)
        {
            int[] page = _list[docid / MAX_SLOTS];
            if (page == null) return defaultValue;

            int val = page[docid % MAX_SLOTS];
            if (val >= 0)
            {
                return val;
            }
            else
            {
                return (val == MISSING ? defaultValue : page[idx - val]);
            }
        }

        public virtual int GetNumItems(int docid)
        {
            int[] page = _list[docid / MAX_SLOTS];
            if (page == null) return 0;

            int slotNo = docid % MAX_SLOTS;
            int val = page[slotNo];

            if (val >= 0) return 1;

            if (val == MISSING) return 0;

            slotNo++;
            while (slotNo < MAX_SLOTS)
            {
                int nextVal = page[slotNo++];
                if (nextVal < 0 && nextVal != MISSING)
                {
                    return (val - nextVal);
                }
            }
            return (val + page.Length);
        }

        public virtual int MaxDoc
        {
            get { return _reader.MaxDoc; }
        }
    }
}
