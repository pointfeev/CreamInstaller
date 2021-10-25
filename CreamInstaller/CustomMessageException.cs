using System;

namespace CreamInstaller
{
    public class CustomMessageException : Exception
    {
        public CustomMessageException(string message) => this.message = message;

        private string message;
        public override string Message => message ?? "CustomMessageException";

        public override string ToString() => Message;
    }
}