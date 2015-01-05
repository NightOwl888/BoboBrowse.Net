// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Query.Scoring
{
    using BoboBrowse.Net.Support;
    using Lucene.Net.Search;

    public class MultiplicativeFacetTermScoringFunction : IFacetTermScoringFunction
    {
        private float _boost = 1.0f;

        public void ClearScores()
        {
            _boost = 1.0f;
        }

        public float Score(int df, float boost)
        {
            return boost;
        }

        public void ScoreAndCollect(int df, float boost)
        {
            if (boost > 0)
            {
                _boost *= boost;
            }
        }

        public float GetCurrentScore()
        {
            return _boost;
        }

        public virtual Explanation Explain(int df, float boost)
        {
            Explanation expl = new Explanation();
            expl.Value = Score(df, boost);
            expl.Description = "boost value of: " + boost;
            return expl;
        }

        public virtual Explanation Explain(params float[] scores)
        {
            Explanation expl = new Explanation();
            float boost = 1.0f;
            foreach (float score in scores)
            {
                boost *= score;
            }
            expl.Value = boost;
            expl.Description = "product of: " + Arrays.ToString(scores);
            return expl;
        }
    }
}
