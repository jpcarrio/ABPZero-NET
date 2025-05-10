using System;

namespace ISUCore.Exceptions
{
    public class EmailDoesntExistException : Exception
    {
        public EmailDoesntExistException() : base() { }
        public EmailDoesntExistException(string message) : base(message) { }
    }
}

