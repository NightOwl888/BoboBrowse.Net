//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
//*
//* Copyright (C) 2005-2015  John Wang
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//*   http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.Search.Section
{
    using Lucene.Net.Index;
    using Lucene.Net.Util;
    using System;

    public class IntMetaDataCache : IMetaDataCache
    {
        private static readonly int MAX_SLOTS = 1024;
        private static readonly int MISSING = int.MinValue;

        private readonly AtomicReader m_reader;
        private int[][] m_list;

        private int m_curPageNo;
        private int[] m_curPage;
        private int m_curSlot;
        private int m_curData;

        public IntMetaDataCache(Term term, AtomicReader reader)
        {
            m_reader = reader;

            int maxDoc = reader.MaxDoc;
            m_list = new int[(maxDoc + MAX_SLOTS - 1) / MAX_SLOTS][];
            m_curPageNo = 0;
            m_curSlot = 0;
            m_curData = MAX_SLOTS;

            if (maxDoc > 0)
            {
                m_curPage = new int[MAX_SLOTS * 2];
                LoadPayload(term);
            }

            m_curPage = null;
        }

        protected virtual void Add(int docid, byte[] data, int blen)
        {
            int pageNo = docid / MAX_SLOTS;
            if (pageNo != m_curPageNo)
            {
                // save the page

                while (m_curSlot < MAX_SLOTS)
                {
                    m_curPage[m_curSlot++] = MISSING;
                }
                m_list[m_curPageNo++] = CopyPage(new int[m_curData]);  // optimize the page to make getMaxItems work
                m_curSlot = 0;
                m_curData = MAX_SLOTS;

                while (m_curPageNo < pageNo)
                {
                    m_list[m_curPageNo++] = null;
                }
            }

            while (m_curSlot < docid % MAX_SLOTS)
            {
                m_curPage[m_curSlot++] = MISSING;
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
                    m_curPage[m_curSlot] = val;
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
            m_curSlot++;
        }

        private void AppendToTail(byte[] data, int blen)
        {
            int ilen = (blen + 3) / 4; // length in ints

            if (m_curPage.Length <= m_curData + ilen)
            {
                // double the size of the variable part at least
                m_curPage = CopyPage(new int[m_curPage.Length + Math.Max((m_curPage.Length - MAX_SLOTS), ilen)]);
            }
            m_curPage[m_curSlot] = (-m_curData);
            m_curData = CopyByteToInt(data, 0, blen, m_curPage, m_curData);
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
            Array.Copy(m_curPage, 0, dst, 0, m_curData);
            return dst;
        }

        protected virtual void LoadPayload(Term term)
        {
            DocsAndPositionsEnum dp = m_reader.GetTermPositionsEnum(term);
            int docID = -1;
            while ((docID = dp.NextDoc()) != DocsEnum.NO_MORE_DOCS)
            {
                if (dp.Freq > 0)
                {
                    dp.NextPosition();
                    BytesRef payload = dp.GetPayload();
                    if (payload != null)
                    {
                        Add(docID, payload.Bytes, payload.Length);
                    }
                }
            }

            // save the last page

            while (m_curSlot < MAX_SLOTS)
            {
                m_curPage[m_curSlot++] = MISSING;
            }
            m_list[m_curPageNo] = CopyPage(new int[m_curData]); // optimize the page to make getNumItems work
            m_curPage = null;
        }

        public virtual int GetValue(int docid, int idx, int defaultValue)
        {
            int[] page = m_list[docid / MAX_SLOTS];
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
            int[] page = m_list[docid / MAX_SLOTS];
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
            get { return m_reader.MaxDoc; }
        }
    }
}
