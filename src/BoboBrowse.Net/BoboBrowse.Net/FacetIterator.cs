// Version compatibility level: 3.1.0
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Support;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Iterator to iterate over facets
    /// author nnarkhed
    /// </summary>
    public abstract class FacetIterator : IIterator<string>
    {
        protected int count;
        protected string facet;

        public virtual int Count 
        { 
            get { return count; } 
        }

        public virtual string Facet
        {
            get { return facet; }
        }

        /// <summary>
        /// Moves the iteration to the next facet
        /// </summary>
        /// <returns>The next facet value.</returns>
        public abstract string Next();

        /// <summary>
        /// Moves the iteration to the next facet whose hitcount >= minHits. returns null if there is no facet whose hitcount >= minHits.
        /// Hence while using this method, it is useless to use hasNext() with it.
        /// After the next() method returns null, calling it repeatedly would result in undefined behavior 
        /// </summary>
        /// <param name="minHits"></param>
        /// <returns>The next facet value. It returns null if there is no facet whose hitcount >= minHits.</returns>
        public abstract string Next(int minHits);

        public abstract string Format(object val);


        // From IIterator<E>

        public abstract bool HasNext();

        public abstract void Remove();
    }
}
