using System;

namespace CreamInstaller
{
    public class CustomMessageException : Exception
    {
        private string message;
        public override string Message => message ?? "CustomMessageException";

        public override string ToString() => Message;

        public CustomMessageException(string message)
        {
            this.message = message;
        }
    }
}