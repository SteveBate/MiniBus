using MiniBus.Contracts;
using System;
using System.Collections.Generic;
using System.Messaging;

namespace MiniBus.Tests.Fakes
{
    public sealed class FakeInvalidMessageQueue : IMessageQueue
    {
        public string FormatName
        {
            get { return "FakeInvalidMessageQueue"; }
        }

        public void Send(Message messgae, string label, System.Messaging.MessageQueueTransactionType transactionType)
        {
        }

        public void ReceiveById(string messageId, System.Messaging.MessageQueueTransactionType transactionType)
        {
        }

        public void ReceiveAsync(Action<Message> current)
        {
        }

        public void StopReceiveAsync()
        {
        }

        public IEnumerable<Message> GetAllMessages()
        {
            return null;
        }

        public Message GetMessageBy(string id)
        {
            return null;
        }

        public bool IsInitialized
        {
            get { return false; }
        }

        public void Dispose()
        {
        }        
    }
}
