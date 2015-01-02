// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Query.Scoring
{
    using BoboBrowse.Net.Support;
    using Lucene.Net.Search;

    public class DefaultFacetTermScoringFunction : IFacetTermScoringFunction
    {
        private float _sum = 0.0f;

        public void ClearScores()
        {
            _sum = 0.0f;
        }

        public float Score(int df, float boost)
        {
            return boost;
        }

        public void ScoreAndCollect(int df, float boost)
        {
            _sum += boost;
        }

        public float GetCurrentScore()
        {
            return _sum;
        }

        public virtual Explanation Explain(int df, float boost)
        {
            Explanation expl = new Explanation();
            expl.Value = Score(df, boost);
            expl.Description = "facet boost value of: " + boost;
            return expl;
        }

        public virtual Explanation Explain(params float[] scores)
        {
            Explanation expl = new Explanation();
            float sum = 0.0f;
            foreach (float score in scores)
            {
                sum += score;
            }
            expl.Value = sum;
            expl.Description = "sum of: " + Arrays.ToString(scores);
            return expl;
        }
    }
}