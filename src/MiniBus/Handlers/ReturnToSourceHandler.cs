using MiniBus.Contracts;
using System.Messaging;

namespace MiniBus.Handlers
{
    internal class ReturnToSourceHandler : IHandleMessage<Message>
    {
        public ReturnToSourceHandler(IReadMessageContext context, ILogMessages logger)
        {
            _context = context;
            _logger = logger;
        }

        public void Handle(Message msg)
        {
            _logger.Log(string.Format("Message: {0} - Removing from queue: {1}", msg.Label, _context.ErrorQueueName));
            _context.ErrorQueue.ReceiveById(msg.Id, MessageQueueTransactionType.Single);
            _logger.Log(string.Format("Message: {0} - Sending to queue: {1}", msg.Label, _context.ReadQueueName));
            _context.ReadQueue.Send(msg, msg.Label, MessageQueueTransactionType.Single);
        }

        readonly IReadMessageContext _context;
        readonly ILogMessages _logger;
    }
}
