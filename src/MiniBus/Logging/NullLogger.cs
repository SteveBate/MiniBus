using MiniBus.Contracts;

namespace MiniBus.Logging
{
    internal class NullLogger : ILogMessages
    {
        public void Log(string message)
        {
            // no op
        }
    }
}