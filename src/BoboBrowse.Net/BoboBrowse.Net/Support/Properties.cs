//* Lesser General Public License for more details.
//*
//* You should have received a copy of the GNU Lesser General Public
//* License along with this library; if not, write to the Free Software
//* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//* 
//* To contact the project administrators for the bobo-browse project, 
//* please go to https://sourceforge.net/projects/bobo-browse/, or 
//* send mail to owner@browseengine.com. 

namespace BoboBrowse.Net.Support
{
    using System.Collections.Generic;

    public class Properties : Dictionary<string, string>
    {
        public string GetProperty(string key)
        {
            string result;

            return TryGetValue(key, out result) ? result : string.Empty;
        }
        public void SetProperty(string key, string value)
        {
            this[key] = value;
        }
    }
}
