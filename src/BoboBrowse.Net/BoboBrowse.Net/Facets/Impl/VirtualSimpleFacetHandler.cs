// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Index;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class VirtualSimpleFacetHandler : SimpleFacetHandler
    {
        protected IFacetDataFetcher _facetDataFetcher;

        public VirtualSimpleFacetHandler(string name,
                                         string indexFieldName,
                                         TermListFactory termListFactory,
                                         IFacetDataFetcher facetDataFetcher,
                                         IEnumerable<string> dependsOn)
            : base(name, null, termListFactory, dependsOn)
        {
            _facetDataFetcher = facetDataFetcher;
        }

        public VirtualSimpleFacetHandler(string name,
                                   TermListFactory termListFactory,
                                   IFacetDataFetcher facetDataFetcher,
                                   IEnumerable<string> dependsOn)
            : this(name, null, termListFactory, facetDataFetcher, dependsOn)
        {
        }

        public override FacetDataCache Load(BoboIndexReader reader)
        {
            int doc = -1;
            C5.TreeDictionary<object, List<int>> dataMap = null;
            List<int> docList = null;

            int nullMinId = -1;
            int nullMaxId = -1;
            int nullFreq = 0;

            TermDocs termDocs = reader.TermDocs(null);
            try
            {
                while (termDocs.Next())
                {
                    doc = termDocs.Doc;
                    object val = _facetDataFetcher.Fetch(reader, doc);
                    if (val == null)
                    {
                        if (nullMinId < 0)
                            nullMinId = doc;
                        nullMaxId = doc;
                        ++nullFreq;
                        continue;
                    }
                    if (dataMap == null)
                    {
                        // Initialize.
                        if (val is long[])
                        {
                            if (_termListFactory == null)
                                _termListFactory = new TermFixedLengthLongArrayListFactory(
                                  ((long[])val).Length);

                            dataMap = new C5.TreeDictionary<object, List<int>>(new VirtualSimpleFacetHandlerLongArrayComparator());
                        }
                        else if (val is IComparable)
                        {
                            dataMap = new C5.TreeDictionary<object, List<int>>();
                        }
                        else
                        {
                            dataMap = new C5.TreeDictionary<object, List<int>>(new VirtualSimpleFacetHandlerObjectComparator());
                        }
                    }

                    if (dataMap.Contains(val))
                        docList = dataMap[val];
                    else
                        docList = null;

                    if (docList == null)
                    {
                        docList = new List<int>();
                        dataMap[val] = docList;
                    }
                    docList.Add(doc);
                }
            }
            finally
            {
                termDocs.Close();
            }
            _facetDataFetcher.Cleanup(reader);

            int maxDoc = reader.MaxDoc;
            int size = dataMap == null ? 1 : (dataMap.Count + 1);

            BigSegmentedArray order = new BigIntArray(maxDoc);
            ITermValueList list = _termListFactory == null ?
              new TermStringList(size) :
              _termListFactory.CreateTermList(size);

            int[] freqs = new int[size];
            int[] minIDs = new int[size];
            int[] maxIDs = new int[size];

            list.Add(null);
            freqs[0] = nullFreq;
            minIDs[0] = nullMinId;
            maxIDs[0] = nullMaxId;

            if (dataMap != null)
            {
                int i = 1;
                int? docId;
                foreach (var entry in dataMap)
                {
                    list.Add(list.Format(entry.Key));
                    docList = entry.Value;
                    freqs[i] = docList.Count;
                    minIDs[i] = docList.Get(0, int.MinValue);
                    while ((docId = docList.Poll(int.MinValue)) != int.MinValue)
                    {
                        doc = (int)docId;
                        order.Add(doc, i);
                    }
                    maxIDs[i] = doc;
                    ++i;
                }
            }
            list.Seal();

            FacetDataCache dataCache = new FacetDataCache(order, list, freqs, minIDs,
              maxIDs, TermCountSize.Large);
            return dataCache;
        }

        public class VirtualSimpleFacetHandlerLongArrayComparator : IComparer<object>
        {
            public int Compare(object big, object small)
            {
                if (((long[])big).Length != ((long[])small).Length)
                {
                    throw new RuntimeException("" + Arrays.ToString((long[])big) + " and " +
                      Arrays.ToString(((long[])small)) + " have different length.");
                }

                long r = 0;
                for (int i = 0; i < ((long[])big).Length; ++i)
                {
                    r = ((long[])big)[i] - ((long[])small)[i];
                    if (r != 0)
                        break;
                }

                if (r > 0)
                    return 1;
                else if (r < 0)
                    return -1;

                return 0;
            }
        }

        public class VirtualSimpleFacetHandlerObjectComparator : IComparer<object>
        {
            public int Compare(object big, object small)
            {
                return string.CompareOrdinal(Convert.ToString(big), Convert.ToString(small));
            }
        }
    }
}
