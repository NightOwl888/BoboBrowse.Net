// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Range
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Query.Scoring;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Index;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class MultiRangeFacetHandler<T> : RangeFacetHandler
    {
        private readonly Term sizePayloadTerm;
        private int maxItems = BigNestedIntArray.MAX_ITEMS;

        public MultiRangeFacetHandler(string name, string indexFieldName, Term sizePayloadTerm,
            TermListFactory termListFactory, IEnumerable<string> predefinedRanges)
            : base(name, indexFieldName, termListFactory, predefinedRanges)
        {
            this.sizePayloadTerm = sizePayloadTerm;
        }

        public override DocComparatorSource GetDocComparatorSource()
        {
            return new MultiValueFacetDataCache<T>.MultiFacetDocCaomparatorSource(new MultiDataCacheBuilder<T>(name, _indexFieldName));
        }

        public override string[] GetFieldValues(BoboIndexReader reader, int id)
        {
            MultiValueFacetDataCache<T> dataCache = GetFacetData(reader);
            if (dataCache != null)
            {
                return dataCache._nestedArray.GetTranslatedData(id, dataCache.ValArray);
            }
            return new string[0];
        }

        public override object[] GetRawFieldValues(BoboIndexReader reader, int id)
        {
 	        MultiValueFacetDataCache<T> dataCache = GetFacetData(reader);
            if (dataCache != null) {
                return dataCache._nestedArray.GetRawData(id, dataCache.ValArray);
            }
            return new String[0];
        }

        public MultiValueFacetDataCache<T> GetFacetData(BoboIndexReader reader)
        {
            return (MultiValueFacetDataCache<T>)reader.GetFacetData(_name);
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string value, Properties prop)
        {
 	         return new FacetRangeFilter<T>(this, value);
        }

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec ospec)
        {
 	         return new MultiRangeFacetCountCollectorSource(this, ospec);
        }

        private class MultiRangeFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly MultiRangeFacetHandler<T> parent;
            private readonly FacetSpec ospec;

            public MultiRangeFacetCountCollectorSource(MultiRangeFacetHandler<T> parent, FacetSpec ospec)
            {
                this.parent = parent;
                this.ospec = ospec;
            }

            public IFacetCountCollector GetFacetCountCollector(BoboIndexReader reader, int docBase)
            {
                MultiValueFacetDataCache<T> dataCache = parent.GetFacetData(reader);
                BigNestedIntArray _nestedArray = dataCache._nestedArray;
                return new MultiRangeFacetCountCollector(parent.Name, dataCache, docBase, this.ospec, parent.PredefinedRanges, parent.Count, _nestedArray);
            }

            public class MultiRangeFacetCountCollector : RangeFacetCountCollector
            {
                private readonly int _count;
                private readonly BigNestedIntArray _nestedArray;

                public MultiRangeFacetCountCollector(string name, MultiValueFacetDataCache<T> dataCache, 
                    int docBase, FacetSpec ospec, IEnumerable<string> predefinedRanges, int count, BigNestedIntArray nestedArray)
                    : base(name, dataCache, docBase, ospec, predefinedRanges)
                {
                    _count = count;
                    _nestedArray = nestedArray;
                }

                public override void Collect(int docid)
                {
                    _nestedArray.CountNoReturn(docid, _count);
                }
            }
        }

        public override BoboDocScorer GetDocScorer(BoboIndexReader reader, DefaultFacetTermScoringFunctionFactory scoringFunctionFactory,
            IDictionary<string, float> boostMap)
        {
            MultiValueFacetDataCache<T> dataCache = GetFacetData(reader);
            float[] boostList = BoboDocScorer.BuildBoostList(dataCache.ValArray, boostMap);
            return new MultiValueDocScorer(dataCache, scoringFunctionFactory, boostList);
        }

        public override MultiValueFacetDataCache<T> Load(BoboIndexReader reader)
        {
 	         return Load(reader, new BoboIndexReader.WorkArea());
        }

        public override MultiValueFacetDataCache<T> Load(BoboIndexReader reader, BoboIndexReader.WorkArea workArea)
        {
            MultiValueFacetDataCache<T> dataCache = new MultiValueFacetDataCache<T>();
            dataCache.SetMaxItems(maxItems);
            if (sizePayloadTerm == null)
            {
                dataCache.Load(_indexFieldName, reader, _termListFactory, workArea);
            }
            else
            {
                dataCache.Load(_indexFieldName, reader, _termListFactory, sizePayloadTerm);
            }
            return dataCache;
        }

        public void SetMaxItems(int maxItems)
        {
            this.maxItems = maxItems;
        }
    }
}
