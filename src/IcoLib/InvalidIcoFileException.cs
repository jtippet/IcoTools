using Ico.Model;
using System;
using System.Runtime.Serialization;

namespace Ico
{
    [Serializable]
    public class InvalidIcoFileException : Exception
    {
        public ParseContext Context { get; set; }

        public InvalidIcoFileException()
        {
        }

        public InvalidIcoFileException(string message) : base(message)
        {
        }

        public InvalidIcoFileException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public InvalidIcoFileException(string message, ParseContext context) : this(message)
        {
            Context = context;
        }

        protected InvalidIcoFileException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override string ToString()
        {
            if (Context != null)
            {
                if (Context.DisplayedPath != null && Context.ImageDirectoryIndex == null)
                {
                    return base.ToString() + $"\nFile: \"{Context.DisplayedPath}\"";
                }
                else if (Context.DisplayedPath != null && Context.ImageDirectoryIndex == null)
                {
                    return base.ToString() + $"\nFile: \"{Context.DisplayedPath}\"";
                }
                else if (Context.DisplayedPath != null && Context.ImageDirectoryIndex.HasValue)
                {
                    return base.ToString() + $"\nFile: \"{Context.DisplayedPath}\"\nImage directory index: #{Context.ImageDirectoryIndex.Value}";
                }
            }
            return base.ToString();
        }
    }
}