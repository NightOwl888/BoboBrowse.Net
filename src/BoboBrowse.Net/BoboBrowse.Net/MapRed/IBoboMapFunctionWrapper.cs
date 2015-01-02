
namespace BoboBrowse.Net.MapRed
{
    using BoboBrowse.Net.Facets;

    /// <summary>
    /// Is the part of the bobo request, that maintains the map result intermediate state
    /// </summary>
    public interface IBoboMapFunctionWrapper
    {
        /// <summary>
        /// When there is no filter, map reduce will try to map the entire segment
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="facetCountCollectors"></param>
        void MapFullIndexReader(BoboIndexReader reader, IFacetCountCollector[] facetCountCollectors);

        /// <summary>
        /// The basic callback method for a single doc
        /// </summary>
        /// <param name="docId"></param>
        /// <param name="reader"></param>
        void MapSingleDocument(int docId, BoboIndexReader reader);

        /// <summary>
        /// The callback method, after the segment was processed
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="facetCountCollectors"></param>
        void FinalizeSegment(BoboIndexReader reader, IFacetCountCollector[] facetCountCollectors);

        /// <summary>
        /// The callback method, after the partition was processed
        /// </summary>
        void FinalizePartition();

        MapReduceResult Result { get; }
    }
}
