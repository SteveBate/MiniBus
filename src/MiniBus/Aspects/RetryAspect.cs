using System;
using System.Messaging;
using MiniBus.Contracts;

namespace MiniBus.Aspects
{
    internal class RetryAspect : IHandleMessage<Message>
    {
        public RetryAspect(IHandleMessage<Message> action, IBusConfig config, ILogMessages logger)
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
            catch(Exception)
            {
                _retry++;
                
                if (_retry <= _config.MaxRetries)
                {
                    _logger.Log(string.Format("Message: {0} - Retry attempt {1}", msg.Label, _retry));
                    Handle(msg);
                }
                else
                {
                    _logger.Log(string.Format("Message: {0} - Invocation failed", msg.Label));
                    throw;
                }
            }
        }
        
        readonly ILogMessages _logger;
        readonly IBusConfig _config;
        readonly IHandleMessage<Message> _inner; 
        int _retry;
    }

}
