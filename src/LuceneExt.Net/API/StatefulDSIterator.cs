
namespace LuceneExt
{
    using System;
    using Lucene.Net.Search;
   
    /// <summary>This abstract class defines methods to iterate over a set of non-decreasing doc ids. </summary>
    public abstract class StatefulDSIterator : DocIdSetIterator
    {
        public abstract int GetCursor();
    }
}
