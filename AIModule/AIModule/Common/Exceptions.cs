using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Common
{
    [Serializable]
    public class ResourceNotFoundException : Exception
    {
        public ResourceNotFoundException() : base() { }
        public ResourceNotFoundException(string message) : base(message) { }
        public ResourceNotFoundException(string message, Exception inner) : base(message, inner) { }
    }

    [Serializable]
    public class InvalidBodyException : Exception
    {
        public InvalidBodyException() : base() { }
        public InvalidBodyException(string message) : base(message) { }
        public InvalidBodyException(string message, Exception inner) : base(message, inner) { }
    }
}
