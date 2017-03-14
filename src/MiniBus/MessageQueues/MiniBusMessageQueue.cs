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
        
        public string FormatName
        {
            get { return _queue.FormatName; }
        }
        
        public void Send(Message message, string label, MessageQueueTransactionType transactionType)
        {
            _queue.Send(message, label, transactionType);

            if (_autoPurgeSystemJournal && (_lastPurgedOn == DateTime.MinValue || _lastPurgedOn.AddMinutes(purgeIntervalInMinutes) < DateTime.Now))
            {
                systemJournalQueue.Purge();
                _lastPurgedOn = DateTime.Now;
            }
        }

        public void ReceiveById(string messageId, MessageQueueTransactionType transactionType)
        {
            _queue.ReceiveById(messageId, transactionType);
        }

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
                    queue.BeginPeek();

                    if (_autoPurgeSystemJournal && (_lastPurgedOn == DateTime.MinValue || _lastPurgedOn.AddMinutes(purgeIntervalInMinutes) < DateTime.Now))
                    {
                        systemJournalQueue.Purge();
                        _lastPurgedOn = DateTime.Now;
                    }
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
            _receiving = false;
        }

        /// <summary>
        /// throws System.Messaging.MessageQueueException
        /// </summary>
        /// <returns>An array of Msmq messages</returns>
        public IEnumerable<Message> GetAllMessages()
        {
            return _queue.GetAllMessages();
        }

        /// <summary>
        /// throws System.Messaging.MessageQueueException
        /// </summary>
        /// <param name="id">the identifier of the message to be moved</param>
        /// <returns>An Msmq message represented by the given id</returns>
        public Message GetMessageBy(string id)
        {
            return _queue.PeekById(id);
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
            if (_disposed) {return;}

            if (disposing)
            {
                if (_queue != null)
                {
                    _queue.Dispose();
                }
            }

            _disposed = true;
        }

        readonly MessageQueue _queue;
        readonly ILogMessages _logger;
        readonly bool _autoPurgeSystemJournal;
        PeekCompletedEventHandler _handler;
        bool _receiving;
        bool _disposed;
        DateTime _lastPurgedOn = DateTime.MinValue;
        const int purgeIntervalInMinutes = 15;

        MessageQueue systemJournalQueue = new MessageQueue("FormatName:Direct=os:.\\System$;JOURNAL");

        
    }
}
