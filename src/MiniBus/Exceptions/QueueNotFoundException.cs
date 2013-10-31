using System;

namespace MiniBus.Exceptions
{
    [Serializable]
    public class QueueNotFoundException : Exception
    {
        public QueueNotFoundException(string message) : base(message)
        {
        }
    }
}
