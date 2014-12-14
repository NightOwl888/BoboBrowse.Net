// Version compatibility level: 3.1.0
namespace BoboBrowse.Net
{
    using System;

    [Serializable]
    public class BrowseException : Exception
    {
        private static long serialVersionUID = 1L;

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
