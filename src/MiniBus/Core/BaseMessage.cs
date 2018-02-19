using System;

namespace MiniBus.Core
{
    public abstract class BaseMessage
    {
        public bool Stop { get; set; }
        public Action<Exception> OnError { get; set; }
        public Action OnStart = delegate { };
        public Action OnComplete = delegate { };
        public Action OnSuccess { get; set; }
        public Action<string> OnStep = delegate { };
    }
}
