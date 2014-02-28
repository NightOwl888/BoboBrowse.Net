//* Lesser General Public License for more details.
//*
//* You should have received a copy of the GNU Lesser General Public
//* License along with this library; if not, write to the Free Software
//* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//* 
//* To contact the project administrators for the bobo-browse project, 
//* please go to https://sourceforge.net/projects/bobo-browse/, or 
//* send mail to owner@browseengine.com. 

namespace BoboBrowse.Net.Facets.Filters
{
    using System;
    using Lucene.Net.Index;
    using Lucene.Net.Search;

    public abstract class RandomAccessFilter : Filter
    {
        public override DocIdSet GetDocIdSet(IndexReader reader)
        {
            return GetRandomAccessDocIdSet(reader);
        }

        public abstract RandomAccessDocIdSet GetRandomAccessDocIdSet(IndexReader reader);
    }
}
