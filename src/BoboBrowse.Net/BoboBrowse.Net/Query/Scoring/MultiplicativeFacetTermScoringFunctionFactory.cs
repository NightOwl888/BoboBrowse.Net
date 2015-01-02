// Version compatibility level: 3.1.0
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
