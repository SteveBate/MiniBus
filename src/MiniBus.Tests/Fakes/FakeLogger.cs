using MiniBus.Contracts;
using System.Collections.Generic;

namespace MiniBus.Tests.Fakes
{
    internal sealed class FakeLogger : ILogMessages
    {
        public FakeLogger()
        {
            _logged = new List<string>();
        }

        public string this[int index] 
        {
            get { return _logged[index]; }
        }

        public void Log(string message)
        {
            _logged.Add(message);
        }

        private List<string> _logged;
    }
}
