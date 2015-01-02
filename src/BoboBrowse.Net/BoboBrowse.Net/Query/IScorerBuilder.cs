// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Query
{
    using Lucene.Net.Index;
    using Lucene.Net.Search;

    public interface IScorerBuilder
    {
        Scorer CreateScorer(Scorer innerScorer, IndexReader reader, bool scoreDocsInOrder, bool topScorer);
        Explanation Explain(IndexReader reader, int doc, Explanation innerExplanation);
    }
}
