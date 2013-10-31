using MiniBus.Contracts;
using System;
using System.Collections.Generic;
using System.Messaging;

namespace MiniBus.Tests.Fakes
{
    public sealed class FakeValidMessageQueue : IMessageQueue
    {
        public void Add(Message message)
        {
            _messages.Add(message);
        }

        public string FormatName
        {
            get { return "FakeValidMessageQueue"; }
        }

        public void Send(Message message, string label, MessageQueueTransactionType transactionType)
        {
            _messages.Add(message);
        }

        public void ReceiveById(string messageId, MessageQueueTransactionType transactionType)
        {
        }

        public void ReceiveAsync(Action<Message> current)
        {
        }

        public IEnumerable<Message> GetAllMessages()
        {
            return _messages;
        }

        public bool IsInitialized
        {
            get { return true; }
        }

        public int Count 
        {
            get { return _messages.Count; }
        }

        private readonly List<Message> _messages = new List<Message>();

        public void Dispose()
        {
        }
    }
}
