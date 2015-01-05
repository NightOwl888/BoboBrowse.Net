//* Lesser General Public License for more details.
//*
//* You should have received a copy of the GNU Lesser General Public
//* License along with this library; if not, write to the Free Software
//* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//* 
//* To contact the project administrators for the bobo-browse project, 
//* please go to https://sourceforge.net/projects/bobo-browse/, or 
//* send mail to owner@browseengine.com. 

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Filter
{
    using BoboBrowse.Net.DocIdSet;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System;
    
    public abstract class RandomAccessFilter : Filter
    {
        //private static long serialVersionUID = 1L; // NOT USED

        public override DocIdSet GetDocIdSet(IndexReader reader)
        {
            if (reader is BoboIndexReader)
            {
                return GetRandomAccessDocIdSet((BoboIndexReader)reader);
            }
            else
            {
                throw new ArgumentException("reader not instance of BoboIndexReader");
            }
        }

        public abstract RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboIndexReader reader);
        public virtual double GetFacetSelectivity(BoboIndexReader reader) { return 0.50; }
    }
}
