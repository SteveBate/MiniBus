using MiniBus.Contracts;
using System;
using System.Collections.Generic;
using System.Messaging;

namespace MiniBus.MessageQueues
{
    internal class MiniBusMessageQueue : IMessageQueue
    {
        public MiniBusMessageQueue(MessageQueue queue, ILogMessages logger)
        {
            _queue = queue;
            _logger = logger;
        }
        
        public string FormatName
        {
            get { return _queue.FormatName; }
        }
        
        public void Send(Message message, string label, MessageQueueTransactionType transactionType)
        {
            _queue.Send(message, label, transactionType);
        }

        public void ReceiveById(string messageId, MessageQueueTransactionType transactionType)
        {
            _queue.ReceiveById(messageId, transactionType);
        }

        public void ReceiveAsync(Action<Message> current)
        {
            if (_receiving) return;
            
            _handler = (source, asyncResult) => {                     
                try
                {
                    _receiving = true;
                    var queue = (MessageQueue)source;
                    Message m = queue.EndPeek(asyncResult.AsyncResult);
                    current(m);
                    queue.BeginPeek();
                }
                catch (MessageQueueException ex)
                {
                    _logger.Log(ex.Message);
                }
            };

            _queue.PeekCompleted += _handler;
            _queue.BeginPeek();
        }

        public void StopReceiveAsync()
        {
            _queue.PeekCompleted -= _handler;
        }

        public IEnumerable<Message> GetAllMessages()
        {
            return _queue.GetAllMessages();
        }

        public bool IsInitialized
        {
            get { return _queue != null; }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MiniBusMessageQueue()
        {
            Dispose(false);
        }

        void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (_queue != null)
                    _queue.Dispose();
            }

            _disposed = true;
        }

        readonly MessageQueue _queue;
        readonly ILogMessages _logger;
        PeekCompletedEventHandler _handler;
        bool _receiving;
        bool _disposed;
    }
}
