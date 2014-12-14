// TODO: Move to support namespace
namespace BoboBrowse.Net
{
    using System;

    [Serializable]
    public class RuntimeException : Exception
    {
        public RuntimeException()
            : base()
        {
        }

        public RuntimeException(string message)
            : base(message)
        {
        }

        public RuntimeException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
