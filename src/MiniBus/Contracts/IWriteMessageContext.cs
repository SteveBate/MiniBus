namespace MiniBus.Contracts
{
    internal interface IWriteMessageContext
    {
        string WriteQueueName { get; }
        IMessageQueue WriteQueue { get; }
    }
}
