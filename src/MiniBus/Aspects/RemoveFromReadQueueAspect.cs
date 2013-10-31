using MiniBus.Contracts;
using System.Messaging;

namespace MiniBus.Aspects
{
    internal class RemoveFromReadQueueAspect : IHandleMessage<Message>
    {
        public RemoveFromReadQueueAspect(IHandleMessage<Message> action, IReadMessageContext context, ILogMessages logger)
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
            finally
            {
                _logger.Log(string.Format("Message: {0} - Removing from read queue: {1}", msg.Label, _context.ReadQueueName));
                _context.ReadQueue.ReceiveById(msg.Id, MessageQueueTransactionType.Single);
            }
        }

        readonly IReadMessageContext _context;
        readonly ILogMessages _logger;
        readonly IHandleMessage<Message> _inner;
    }
}
