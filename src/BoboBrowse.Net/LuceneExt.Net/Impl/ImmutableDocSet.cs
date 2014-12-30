﻿// Kamikaze version compatibility level: 3.0.6
namespace LuceneExt.Impl
{
    using System;
    using System.IO;
    using Lucene.Net.Search;

    public abstract class ImmutableDocSet : DocSet
    {
        private int size = -1;

        public override void AddDoc(int docid)
        {
            throw new NotSupportedException("Attempt to add document to an immutable data structure");
        }

        public override int Size()
        {
            // Do the size if we haven't done it so far.
            if (size < 0)
            {
                DocIdSetIterator dcit = Iterator();
                size = 0;
                try
                {
                    while (dcit.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        size++;
                    }
                }
                catch
                {                    
                    return -1;
                }
            }
            return size;
        }
    }
}
