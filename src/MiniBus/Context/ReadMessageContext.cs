using MiniBus.Contracts;

namespace MiniBus.Context
{
    internal class ReadMessageContext : IReadMessageContext
    {
        public ReadMessageContext(IMessageQueue errorQueue, IMessageQueue readQueue)
        {      
            _errorQueue = errorQueue;
            _readQueue = readQueue;
        }

        public IMessageQueue ErrorQueue
        {
            get { return _errorQueue; }
        }

        public string ErrorQueueName
        {
            get { return _errorQueue.FormatName; }
        }

        public IMessageQueue ReadQueue
        {
            get { return _readQueue; }
        }

        public string ReadQueueName
        {
            get { return _readQueue.FormatName; }
        }

        readonly IMessageQueue _errorQueue;
        readonly IMessageQueue _readQueue;
    }
}
