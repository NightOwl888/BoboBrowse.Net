// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Query.Scoring
{
    public class MultiplicativeFacetTermScoringFunctionFactory : IFacetTermScoringFunctionFactory
    {
        public virtual IFacetTermScoringFunction GetFacetTermScoringFunction(int termCount, int docCount)
        {
            return new MultiplicativeFacetTermScoringFunction();
        }
    }
}
