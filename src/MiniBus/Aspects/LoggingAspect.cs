using System;
using System.Messaging;
using MiniBus.Contracts;

namespace MiniBus.Aspects
{
    internal class LoggingAspect : IHandleMessage<Message>
    {
        public LoggingAspect(IHandleMessage<Message> action, string operation, ILogMessages logger)
        {
            _inner = action;
            _logger = logger;
            _operation = operation;
        }

        public void Handle(Message msg)
        {
            _logger.Log(string.Format("Message: {0} - Started {1} Operation", msg.Label, _operation));
            try
            {
                _inner.Handle(msg);
            }
            catch (Exception ex)
            {
                _logger.Log(string.Format("Message: {0} - EXCEPTION {1}", msg.Label, ex.Message));
                _logger.Log(string.Format("Message: {0} - {1}", msg.Label, ex));
                throw;
            }
            finally
            {
                _logger.Log(string.Format("Message: {0} - Completed {1} Operation", msg.Label, _operation));
            }
        }

        readonly IHandleMessage<Message> _inner;
        readonly ILogMessages _logger;        
        readonly string _operation;
    }
}
