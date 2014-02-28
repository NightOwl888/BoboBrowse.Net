

namespace BoboBrowse.Net
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class BrowseException : Exception
    {
        public BrowseException(string msg)
            : this(msg, null)
        {
        }

        public BrowseException(string msg, System.Exception cause)
            : base(msg, cause)
        {
        }
    }
}
