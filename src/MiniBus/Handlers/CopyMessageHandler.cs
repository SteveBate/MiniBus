using MiniBus.Contracts;
using System.Messaging;

namespace MiniBus.Handlers
{
    internal class CopyMessageHandler : IHandleMessage<Message>
    {
        public CopyMessageHandler(IWriteMessageContext toContext, IBusConfig config, ILogMessages logger)
        {
            _toContext = toContext;
            _config = config;
            _logger = logger;
        }

        public void Handle(Message msg)
        {
            //_logger.Log(string.Format("Message: {0} - Copying to queue: {1}", msg.Label, _toContext.WriteQueueName));
            _toContext.WriteQueue.Send(msg, msg.Label, GetTransactionType(_config.EnlistInAmbientTransactions));
        }

        MessageQueueTransactionType GetTransactionType(bool ambient)
        {
            return ambient ? MessageQueueTransactionType.Automatic : MessageQueueTransactionType.Single;
        }

        readonly IWriteMessageContext _toContext;
        readonly IBusConfig _config;
        readonly ILogMessages _logger;
    }
}
