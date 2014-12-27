namespace BoboBrowse.Net.Facets.Statistics
{
    using System;
    using System.Text;

    [Serializable]
    public class FacetCountStatistics
    {
        private double _distribution;
        private int _totalSampleCount;
        private int _collectedSampleCount;
        private int _numSamplesCollected;

        public virtual double getDistribution()
        {
            return _distribution;
        }

        public virtual void setDistribution(double distribution)
        {
            _distribution = distribution;
        }

        public virtual int getTotalSampleCount()
        {
            return _totalSampleCount;
        }

        public virtual void setTotalSampleCount(int totalSampleCount)
        {
            _totalSampleCount = totalSampleCount;
        }

        public virtual int getCollectedSampleCount()
        {
            return _collectedSampleCount;
        }

        public virtual void setCollectedSampleCount(int collectedSampleCount)
        {
            _collectedSampleCount = collectedSampleCount;
        }

        public virtual int getNumSamplesCollected()
        {
            return _numSamplesCollected;
        }

        public virtual void setNumSamplesCollected(int numSamplesCollected)
        {
            _numSamplesCollected = numSamplesCollected;
        }

        public override string ToString()
        {
            StringBuilder buf = new StringBuilder();
            buf.Append("num samples collected: ").Append(_numSamplesCollected);
            buf.Append("\ncollected sample count: ").Append(_collectedSampleCount);
            buf.Append("\ntotal samples count: ").Append(_totalSampleCount);
            buf.Append("\ndistribution score: ").Append(_distribution);
            return buf.ToString();
        }

        public override bool Equals(object o)
        {
            bool ret = false;
            if (o is FacetCountStatistics)
            {
                FacetCountStatistics stat = (FacetCountStatistics)o;
                if (_collectedSampleCount == stat._collectedSampleCount && _numSamplesCollected == stat._numSamplesCollected && _totalSampleCount == stat._totalSampleCount && _distribution == stat._distribution)
                {
                    ret = true;
                }
            }
            return ret;
        }
    }
}