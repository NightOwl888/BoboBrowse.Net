namespace BoboBrowse.Net.Service
{
    using System.Collections.Generic;

    public class HitCompareMulti : IComparer<BrowseHit>
    {
        private IComparer<BrowseHit>[] hcmp;

        public HitCompareMulti(IComparer<BrowseHit>[] hcmp)
        {
            this.hcmp = hcmp;
        }

        // HitCompare
        public virtual int Compare(BrowseHit h1, BrowseHit h2)
        {
            int retVal = 0;
            for (int i = 0; i < hcmp.Length; ++i)
            {
                retVal = hcmp[i].Compare(h1, h2);
                if (retVal != 0)
                    break;
            }
            return retVal;
        }
    }
}