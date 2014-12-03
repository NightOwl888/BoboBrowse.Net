namespace BoboBrowse.Net.Query.Scoring
{
    using Lucene.Net.Search;

    public interface IBoboFacetTermQueryBuilder
	{
		Query BuildFacetTermQuery(IFacetTermScoringFunctionFactory scoreFunctionFactory);
	}
}