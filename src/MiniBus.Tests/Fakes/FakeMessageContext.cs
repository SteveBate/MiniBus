using MiniBus.Contracts;

namespace MiniBus.Tests.Fakes
{
    internal class FakeMessageContext
    {
        public string ErrorQueueName => "TestErrorQueue";

        public string ReadQueueName => "TestReadQueue";

        public string WriteQueueName => "TestWriteQueue";

        public IMessageQueue ErrorQueue => new FakeValidMessageQueue("errorQueue");

        public IMessageQueue ReadQueue => new FakeValidMessageQueue("readQueue");

        public IMessageQueue WriteQueue => new FakeValidMessageQueue("writeQueue1");
    }
}
