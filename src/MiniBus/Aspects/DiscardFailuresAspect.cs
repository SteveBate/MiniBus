using MiniBus.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Text;

namespace MiniBus.Aspects
{
    class DiscardFailuresAspect : IHandleMessage<Message>
    {
        public DiscardFailuresAspect(IHandleMessage<Message> action, IBusConfig config, ILogMessages logger)
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
            catch (Exception ex)
            {
                if (_config.DiscardFailures)
                {
                    _logger.Log(string.Format("Message: {0} - Payload discarded as determined by DiscardFailures flag", msg.Label, ex.Message));
                }
                else
                {
                    throw;
                }
            }
        }

        readonly ILogMessages _logger;
        readonly IBusConfig _config;
        readonly IHandleMessage<Message> _inner; 
    }
}
