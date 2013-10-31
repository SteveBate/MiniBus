namespace MiniBus.Contracts
{
    internal interface IReadMessageContext
    {
        string ErrorQueueName { get; }
        string ReadQueueName { get; }
        IMessageQueue ErrorQueue { get; }
        IMessageQueue ReadQueue { get; }
    }
}
