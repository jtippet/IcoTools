using System;
using System.Runtime.Serialization;

namespace Ico
{
    public class InvalidPngFileException : Exception
    {
        public InvalidPngFileException()
        {
        }

        public InvalidPngFileException(string message) : base(message)
        {
        }

        public InvalidPngFileException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidPngFileException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
