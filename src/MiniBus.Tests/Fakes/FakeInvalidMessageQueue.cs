using System;
using System.Collections.Generic;
using MSMQ.Messaging;
using MiniBus.Contracts;

namespace MiniBus.Tests.Fakes
{
    public sealed class FakeInvalidMessageQueue : IMessageQueue
    {
        public string FormatName => "FakeInvalidMessageQueue";

        public void Send(Message messgae, string label, MessageQueueTransactionType transactionType) {}

        public void ReceiveById(string messageId, MessageQueueTransactionType transactionType) {}

        public void ReceiveAsync(Action<Message> current) {}

        public void StopReceiveAsync() {}

        public IEnumerable<Message> PeekAllMessages() => null;

        public Message PeekMessageBy(string id) => null;

        public bool IsInitialized => false;

        public void Dispose() {}
    }
}
