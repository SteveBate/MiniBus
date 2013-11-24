using MiniBus.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Text;

namespace MiniBus.Aspects
{
    internal class FailFastAspect : IHandleMessage<Message>
    {
        public FailFastAspect(IHandleMessage<Message> action, IBusConfig config, ILogMessages logger)
        {
            _inner = action;
            _config = config;
            _logger = logger;
        }

        public void Handle(Message msg)
        {
            try
            {
                _inner.Handle(msg);
            }
            catch (Exception)
            {
                Failed = _config.FailFast;
            }
        }

        public bool Failed { get; private set; }

        readonly ILogMessages _logger;
        readonly IBusConfig _config;
        readonly IHandleMessage<Message> _inner; 
    }
}
