// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Support
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
