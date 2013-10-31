using MiniBus.Contracts;

namespace MiniBus.Tests.Fakes
{
    internal class FakeMessageContext : IReadMessageContext, IWriteMessageContext
    {
        public string ErrorQueueName
        {
            get { return "TestErrorQueue"; }
        }

        public string ReadQueueName
        {
            get { return "TestReadQueue"; }
        }

        public string WriteQueueName
        {
            get { return "TestWriteQueue"; }
        }

        public IMessageQueue ErrorQueue
        {
            get { return new FakeValidMessageQueue(); }
        }

        public IMessageQueue ReadQueue
        {
            get { return new FakeValidMessageQueue(); }
        }

        public IMessageQueue WriteQueue
        {
            get { return new FakeValidMessageQueue(); }
        }
    }
}
