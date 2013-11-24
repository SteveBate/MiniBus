using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using MiniBus.Aspects;
using MiniBus.Contracts;
using MiniBus.Exceptions;
using MiniBus.Handlers;
using MiniBus.Context;
using MiniBus.Formatters;
using MiniBus.MessageQueues;

namespace MiniBus
{
    internal class Bus : IBus
    {
        public Bus(IBusConfig config, ILogMessages logger, IMessageQueue errorQueue, IMessageQueue readQueue, IEnumerable<IMessageQueue> writeQueues)
        {
            _config = config;
            _logger = logger;
            _errorQueue = errorQueue;
            _readQueue = readQueue;
            _writeQueueManager = new WriteQueueManager(_config.AutoDistributeOnSend, writeQueues);
        }      

        /// <summary>
        /// Takes a given type T and serializes it to json before
        /// wrapping it in an MSMQ message and placing it on a queue.
        /// </summary>
        public void Send<T>(T dto)
        {
            if (!_writeQueueManager.HasWriteQueues)
                throw new BusException("Bus has not been configured for sending messages. Did you forget to call DefineWriteQueue on BusBuilder?");

            foreach (var writeQueue in _writeQueueManager.GetWriteQueues())
            {
                var message = CreateMsmqMessageFromDto(dto);
                var context = new WriteMessageContext(writeQueue);
                var sendMessageHandler = new SendMessageHandler(context, _config, _logger);
                var loggingAspect = new LoggingAspect(sendMessageHandler, SendOperation, _logger);
                var transactionAspect = new TransactionAspect(loggingAspect, _logger);                
                transactionAspect.Handle(message);                
            }
        }
        
        /// <summary>
        /// Reads messages off a queue, deserializes them into the 
        /// specified type T and invokes registered handlers. Useful when
        /// you want to control when messages are processed i.e. at a set
        /// time every day, for example.
        /// </summary>
        public void Receive<T>()
        {
            if (_readQueue == null || !_readQueue.IsInitialized) 
                throw new BusException("Bus has not been configured for receiving messages. Did you forget to call DefineReadQueue on BusBuilder?");

            foreach (Message message in _readQueue.GetAllMessages())
            {                
                var context = new ReadMessageContext(_errorQueue, _readQueue);
                var receiveMessageHandler = new ReceiveMessageHandler<T>(_handlers, _config, _logger);
                var retryAspect = new RetryAspect(receiveMessageHandler, _config, _logger);
                var removeFromReadQueueAspect = new RemoveFromReadQueueAspect(retryAspect, context, _config, _logger);
                var moveToErrorQueueAspect = new MoveToErrorQueueAspect(removeFromReadQueueAspect, context, _config, _logger);
                var loggingAspect = new LoggingAspect(moveToErrorQueueAspect, ReceiveOperation, _logger);
                var transactionAspect = new TransactionAspect(loggingAspect, _logger);
                var failFastAspect = new FailFastAspect(transactionAspect, _config, _logger);
                failFastAspect.Handle(message);

                if (failFastAspect.Failed)
                    break;
            }            
        }

        /// <summary>
        /// Reads messages off a queue as they arrive, deserializes them into the
        /// specified type T and invokes registered handlers. This operation is
        /// asynchnronous meaning registered handlers will be invoked on the
        /// background thread.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void ReceiveAsync<T>()
        {
            if (_readQueue == null || !_readQueue.IsInitialized)
                throw new BusException("Bus has not been configured for receiving messages. Did you forget to call DefineReadQueue on BusBuilder?");

            _readQueue.ReceiveAsync(message => {

                var context = new ReadMessageContext(_errorQueue, _readQueue);
                var receiveMessageHandler = new ReceiveMessageHandler<T>(_handlers, _config, _logger);
                var retryAspect = new RetryAspect(receiveMessageHandler, _config, _logger);
                var removeFromReadQueueAspect = new RemoveFromReadQueueAspect(retryAspect, context,_config, _logger);
                var moveToErrorQueueAspect = new MoveToErrorQueueAspect(removeFromReadQueueAspect, context, _config, _logger);
                var loggingAspect = new LoggingAspect(moveToErrorQueueAspect, ReceiveOperation, _logger);
                var transactionAspect = new TransactionAspect(loggingAspect, _logger);
                var failFastAspect = new FailFastAspect(transactionAspect, _config, _logger);
                failFastAspect.Handle(message);

                if (failFastAspect.Failed)
                    _readQueue.StopReceiveAsync();
            });
        }

        /// <summary>
        /// Read messages containg the given type T off the defined 
        /// error queue and moves them to the user defined read queue
        /// </summary>
        public void ReturnErrorMessages()
        {
            if ((_errorQueue == null || !_errorQueue.IsInitialized) || (_readQueue == null || !_readQueue.IsInitialized))
                throw new BusException("Bus has not been configured for returning messages to the read queue. Did you forget to call DefineReadQueue and/or DeineErrorQueue on BusBuilder?");

            foreach (Message message in _errorQueue.GetAllMessages())
            {
                var context = new ReadMessageContext(_errorQueue, _readQueue);
                var returnToSourceHandler = new ReturnToSourceHandler(context, _logger);
                var loggingAspect = new LoggingAspect(returnToSourceHandler, ReturnOperation, _logger);
                loggingAspect.Handle(message);
            }            
        }

        /// <summary>
        /// Takes user-defined message handlers implementing the IHandleMessage 
        /// of T interface and stores them to be invoked later when the Receive
        /// method is called.
        /// </summary>
        public void RegisterHandler<T>(IHandleMessage<T> handler)
        {
            _handlers.Add(new Action<T>(handler.Handle));
        }

        Message CreateMsmqMessageFromDto<T>(T dto)
        {
            return new Message
            {
                UseAuthentication = false,
                Recoverable = true,
                Body = dto,
                AcknowledgeType = AcknowledgeTypes.FullReachQueue | AcknowledgeTypes.FullReceive,
                UseJournalQueue = true,
                Formatter = _config.JsonSerialization ? (IMessageFormatter)new JsonFormatter<T>() : new XmlMessageFormatter(new[] { typeof(T) }),
                Label = Guid.NewGuid().ToString(),
            };
        }
        
        ~Bus()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _writeQueueManager.Dispose();

                if (_readQueue != null)
                    _readQueue.Dispose();
                if (_errorQueue != null)
                    _errorQueue.Dispose();
            }

            _disposed = true;
        }

        readonly WriteQueueManager _writeQueueManager;
        readonly IMessageQueue _readQueue;
        readonly IMessageQueue _errorQueue;
        readonly IBusConfig _config;
        readonly ILogMessages _logger;
        const string SendOperation = "SEND";
        const string ReceiveOperation = "RECEIVE";
        const string ReturnOperation = "RETURN_TO_SOURCE";
        readonly List<Delegate> _handlers = new List<Delegate>();
        bool _disposed;
    }
}
