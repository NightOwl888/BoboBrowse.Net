namespace BoboBrowse.Net.Query.Scoring
{
	public class DefaultFacetTermScoringFunctionFactory : IFacetTermScoringFunctionFactory
	{
		public virtual IFacetTermScoringFunction GetFacetTermScoringFunction(int termCount, int docCount)
		{
			return new DefaultFacetTermScoringFunction();
		}
	}
}