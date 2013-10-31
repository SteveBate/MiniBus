using System;
using System.Messaging;
using MiniBus.Contracts;

namespace MiniBus.Aspects
{
    internal class MoveToErrorQueueAspect : IHandleMessage<Message>
    {
        public MoveToErrorQueueAspect(IHandleMessage<Message> action, IReadMessageContext context, ILogMessages logger)
        {
            _context = context;
            _logger = logger;
            _inner = action;
        }

        public void Handle(Message msg)
        {
            try
            {
                _inner.Handle(msg);
            }
            catch (Exception)
            {
                _logger.Log(string.Format("Message: {0} - Moving to error queue: {1}", msg.Label, _context.ErrorQueueName));
                _context.ErrorQueue.Send(msg, msg.Label, MessageQueueTransactionType.Single);
                throw;
            }
        }

        readonly IReadMessageContext _context;
        readonly ILogMessages _logger;
        readonly IHandleMessage<Message> _inner;
    }
}
