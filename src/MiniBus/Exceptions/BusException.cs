using System;

namespace MiniBus.Exceptions
{
    [Serializable]
    public class BusException : Exception
    {
        public BusException(string message) : base(message)
        {
        }
    }
}