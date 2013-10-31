using MiniBus.Contracts;
using System.Messaging;

namespace MiniBus.Handlers
{
    internal class SendMessageHandler : IHandleMessage<Message>
    {
        public SendMessageHandler(IWriteMessageContext context, IBusConfig config, ILogMessages logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        public void Handle(Message msg)
        {
            _logger.Log(string.Format("Message: {0} - Payload: {1}", msg.Label, msg.Body));
            _context.WriteQueue.Send(msg, msg.Label, GetTransactionType(_config.EnlistInAmbientTransactions));
            _logger.Log(string.Format("Message: {0} - Sent to queue: {1}", msg.Label, _context.WriteQueueName));        
        }

        MessageQueueTransactionType GetTransactionType(bool ambient)
        {
            return ambient ? MessageQueueTransactionType.Automatic : MessageQueueTransactionType.Single;
        }

        readonly IWriteMessageContext _context;
        readonly IBusConfig _config;
        readonly ILogMessages _logger;
    }
}