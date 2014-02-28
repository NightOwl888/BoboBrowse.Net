
namespace BoboBrowse.Net
{
    using System;

    public class RuntimeException : Exception
    {
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
