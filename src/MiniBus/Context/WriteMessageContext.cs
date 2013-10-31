using MiniBus.Contracts;

namespace MiniBus.Context
{
    internal class WriteMessageContext : IWriteMessageContext
    {
        public WriteMessageContext(IMessageQueue writeQueue)
        {      
            _writeQueue = writeQueue;
        }

        public IMessageQueue WriteQueue
        {
            get { return _writeQueue; }
        }

        public string WriteQueueName
        {
            get { return _writeQueue.FormatName; }
        }

        readonly IMessageQueue _writeQueue;
    }
}
