﻿// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Sort
{
    using Lucene.Net.Index;
    using System;

    public class MultiDocIdComparatorSource : DocComparatorSource
    {
        private DocComparatorSource[] _compSources;

        public MultiDocIdComparatorSource(DocComparatorSource[] compSources)
        {
            _compSources = compSources;
        }

        public override DocComparator GetComparator(IndexReader reader, int docbase)
        {
            DocComparator[] comparators = new DocComparator[_compSources.Length];
            for (int i = 0; i < _compSources.Length; ++i)
            {
                comparators[i] = _compSources[i].GetComparator(reader, docbase);
            }
            return new MultiDocIdComparator(comparators);
        }
    }
}
