using MiniBus.Contracts;
using System;
using System.Linq;
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
            var msg = _messages.Find(m => m.Id == messageId);
            _messages.Remove(msg);
        }

        public void ReceiveAsync(Action<Message> current)
        {
        }

        public void StopReceiveAsync()
        {
        }

        public IEnumerable<Message> GetAllMessages()
        {
            var newList = new List<Message>();
            _messages.ForEach(m => newList.Add(m));
            return newList;
        }

        public Message GetMessageBy(string id)
        {
            return _messages.SingleOrDefault(m => m.Label == id);
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
