using System;
using System.Collections.Generic;
using System.Messaging;

namespace MiniBus.Contracts
{
    internal interface IMessageQueue : IDisposable
    {
        string FormatName { get; }        
        bool IsInitialized { get; }
        void Send(Message message, string label, MessageQueueTransactionType transactionType);
        void ReceiveById(string messageId, MessageQueueTransactionType transactionType);
        void ReceiveAsync(Action<Message> current);
        void StopReceiveAsync();
        IEnumerable<Message> PeekAllMessages();
        Message PeekMessageBy(string id);
    }
}
