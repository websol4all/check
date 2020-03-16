using System;

namespace Xyzies.Devices.Services.Exceptions
{
    public class AccessException : ApplicationException
    {
        public AccessException() : base() { }
        public AccessException(string message) : base(message) { }
    }
}
