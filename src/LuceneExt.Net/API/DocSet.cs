
namespace LuceneExt
{
    using System;
    using Lucene.Net.Search;

    public abstract class DocSet : DocIdSet
    {        
        /// <summary>
        /// Add a doc id to the set 
        /// </summary>
        /// <param name="docid">The doc id to add.</param>
        public abstract void AddDoc(int docid);

        ///<summary>Return the set size </summary>
        ///<returns>true if present, false otherwise </returns>
        public virtual bool Find(int val)
        {
            return FindWithIndex(val) > -1 ? true : false;
        }

        ///<summary>Return the set size </summary>
        ///<returns>index if present, -1 otherwise </returns>
        public virtual int FindWithIndex(int val)
        {
            return -1;
        }

        ///<summary>Gets the number of ids in the set </summary>
        ///<returns>size of the docset </returns>
        public virtual int Size()
        {
            return 0;
        }

        ///<summary>Return the set size in bytes </summary>
        ///<returns>index if present, -1 otherwise </returns>
        public virtual long SizeInBytes()
        {
            return 0;
        }

        ///<summary>Optimize by trimming underlying data structures </summary>
        public virtual void Optimize()
        {
            return;
        }
    }
}
