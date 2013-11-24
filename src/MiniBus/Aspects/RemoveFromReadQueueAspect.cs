using MiniBus.Contracts;
using System;
using System.Messaging;

namespace MiniBus.Aspects
{
    internal class RemoveFromReadQueueAspect : IHandleMessage<Message>
    {
        public RemoveFromReadQueueAspect(IHandleMessage<Message> action, IReadMessageContext context, IBusConfig config, ILogMessages logger)
        {
            _context = context;
            _config = config; 
            _logger = logger;
            _inner = action;
        }

        public void Handle(Message msg)
        {
            try
            {
                _inner.Handle(msg);
                _logger.Log(string.Format("Message: {0} - Removing from read queue: {1}", msg.Label, _context.ReadQueueName));
                _context.ReadQueue.ReceiveById(msg.Id, MessageQueueTransactionType.Single);
            }
            catch
            {
                if (!_config.FailFast)
                {
                    _logger.Log(string.Format("Message: {0} - Removing from read queue: {1}", msg.Label, _context.ReadQueueName));
                    _context.ReadQueue.ReceiveById(msg.Id, MessageQueueTransactionType.Single);
                }

                throw;
            }
        }

        readonly IReadMessageContext _context;
        readonly IBusConfig _config;
        readonly ILogMessages _logger;
        readonly IHandleMessage<Message> _inner;
    }
}
