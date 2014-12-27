namespace BoboBrowse.Net.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// An iterator over a collection. Iterator takes the place of Enumeration in the Java Collections Framework. Iterators differ from enumerations in two ways:
    /// <list type="">
    ///     <item>Iterators allow the caller to remove elements from the underlying collection during the iteration with well-defined semantics.</item>
    ///     <item>Method names have been improved.</item>
    /// </list>
    ///     
    /// This interface is a member of the Java Collections Framework.
    /// </summary>
    /// <typeparam name="E"></typeparam>
    public interface IIterator<E>
    {
        /// <summary>
        /// Returns true if the iteration has more elements.
        /// </summary>
        /// <returns></returns>
        bool HasNext();

        /// <summary>
        /// Returns the next element in the iteration.
        /// </summary>
        /// <returns></returns>
        E Next();

        /// <summary>
        /// Removes from the underlying collection the last element returned by this iterator (optional operation).
        /// </summary>
        void Remove();
    }
}
