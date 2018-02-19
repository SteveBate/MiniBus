using MiniBus.Contracts;
using System;
using System.Collections.Generic;
using System.Messaging;

namespace MiniBus.MessageQueues
{
    internal class MiniBusMessageQueue : IMessageQueue
    {
        public MiniBusMessageQueue(MessageQueue queue, ILogMessages logger, bool autoPurgeSystemJournal)
        {
            _queue = queue;
            _logger = logger;
            _autoPurgeSystemJournal = autoPurgeSystemJournal;
        }
        
        public string FormatName => _queue.FormatName;

        public bool IsInitialized => _queue != null;

        public void Send(Message message, string label, MessageQueueTransactionType transactionType)
        {
            _queue.Send(message, label, transactionType);

            if (_autoPurgeSystemJournal && (_lastPurgedOn == DateTime.MinValue || _lastPurgedOn.AddMinutes(PurgeIntervalInMinutes) < DateTime.Now))
            {
                _systemJournalQueue.Purge();
                _lastPurgedOn = DateTime.Now;
            }
        }

        public void ReceiveById(string messageId, MessageQueueTransactionType transactionType) => _queue.ReceiveById(messageId, transactionType);

        public void ReceiveAsync(Action<Message> current)
        {
            if (_receiving) {return;}
            
            _handler = (source, asyncResult) => {                     
                try
                {
                    _receiving = true;
                    var queue = (MessageQueue)source;
                    using (Message m = queue.EndPeek(asyncResult.AsyncResult))
                    {
                        current(m);
                    }

                    // In the event of environmental issues such as sql server timeouts or connection refused errors the process would sometimes stop consuming MSMQ messages
                    // Recreating the MessageQueue object and re-attaching the PeekCompleted event handler fixes this. The operation is fast and cheap and most importantly, works.
                    _queue.PeekCompleted -= _handler;
                    _queue = new MessageQueue(queue.Path);
                    _queue.PeekCompleted += _handler;
                    _queue.BeginPeek();

                    if (_autoPurgeSystemJournal && (_lastPurgedOn == DateTime.MinValue || _lastPurgedOn.AddMinutes(PurgeIntervalInMinutes) < DateTime.Now))
                    {
                        _systemJournalQueue.Purge();
                        _lastPurgedOn = DateTime.Now;
                    }
                }
                catch (MessageQueueException ex)
                {
                    _logger.Log(ex.ToString());
                    _logger.Log("Restarting ReceiveAsync...");
                    _queue.BeginPeek();
                    _logger.Log("Waiting for message after exception...");
                }
            };

            _queue.PeekCompleted += _handler;
            _queue.BeginPeek();
            _logger.Log("Waiting for message...");
        }

        public void StopReceiveAsync()
        {
            _queue.PeekCompleted -= _handler;
            _receiving = false;
        }

        /// <summary>
        /// throws System.Messaging.MessageQueueException
        /// </summary>
        /// <returns>An array of Msmq messages</returns>
        public IEnumerable<Message> PeekAllMessages() => _queue.GetAllMessages();

        /// <summary>
        /// throws System.Messaging.MessageQueueException
        /// </summary>
        /// <param name="id">the identifier of the message to be moved</param>
        /// <returns>An Msmq message represented by the given id</returns>
        public Message PeekMessageBy(string id) => _queue.PeekById(id);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MiniBusMessageQueue()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) {return;}

            if (disposing)
            {
                _queue?.Dispose();
                _systemJournalQueue.Dispose();
            }

            _disposed = true;
        }

        private readonly MessageQueue _systemJournalQueue = new MessageQueue("FormatName:Direct=os:.\\System$;JOURNAL");
        private MessageQueue _queue;
        private readonly ILogMessages _logger;
        private readonly bool _autoPurgeSystemJournal;
        private DateTime _lastPurgedOn = DateTime.MinValue;
        private PeekCompletedEventHandler _handler;
        private bool _receiving;
        private bool _disposed;
        private const int PurgeIntervalInMinutes = 15;
    }
}
