using MiniBus.Contracts;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Messaging;

namespace MiniBus.Tests.Fakes
{
    public sealed class FakeValidMessageQueue : IMessageQueue
    {
        public FakeValidMessageQueue(string queueName) => FormatName = queueName;

        public void Add(Message message) => _messages.Add(message);

        public string FormatName { get; }

        public void Send(Message message, string label, MessageQueueTransactionType transactionType) => _messages.Add(message);

        public void ReceiveById(string messageId, MessageQueueTransactionType transactionType)
        {
            var msg = _messages.Find(m => m.Id == messageId);
            _messages.Remove(msg);
        }

        public void ReceiveAsync(Action<Message> current) {}

        public void StopReceiveAsync() {}

        public IEnumerable<Message> PeekAllMessages()
        {
            var newList = new List<Message>();
            _messages.ForEach(m => newList.Add(m));
            return newList;
        }

        public Message PeekMessageBy(string id) => _messages.SingleOrDefault(m => m.Label == id);

        public bool IsInitialized => true;

        public int Count => _messages.Count;

        public void Dispose() {}

        readonly List<Message> _messages = new List<Message>();
    }
}
