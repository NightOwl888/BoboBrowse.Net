namespace BoboBrowse.Net.Query.Scoring
{
    using BoboBrowse.Net.Util;
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
            Explanation expl = new Explanation(Score(df, boost), "boost value of: " + boost);
            return expl;
        }

        public virtual Explanation Explain(params float[] scores)
        {
            float sum = 0.0f;
            foreach (float score in scores)
            {
                sum += score;
            }
            Explanation expl = new Explanation(sum, "sum of: " + Arrays.ToString(scores));
            return expl;
        }
    }
}