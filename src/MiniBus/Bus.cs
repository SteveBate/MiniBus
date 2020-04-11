using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using MiniBus.Aspects;
using MiniBus.Contracts;
using MiniBus.Exceptions;
using MiniBus.Core;
using MiniBus.Filters;
using MiniBus.Formatters;

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
            _writeQueues = writeQueues;
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

        /// <summary>
        /// Takes a given type T and serializes it to json before wrapping it in an MSMQ message and placing it on a queue.
        /// For the lifetime of the bus, if muliple queues are defined then each time Send is invoked:
        /// a) if a destination has been passed in, send to that queue only
        /// b) or if AutoDistributeOnSend is true then a message is placed on the next queue in the list in a round-robin fashion
        /// c) otherwise a message is placed on all queues
        /// </summary>
        public void Send<T>(T dto, string destination = "")
        {
            GuardAgainstInvalidWriteQueues();
            GuardAgainstInvalidErrorQueue();

            destination = stripRemoteFrom(destination);

            // configure the pipeline for sending a message
            var pipe = new PipeLine<MessageContext>();
            pipe.AddAspect(new TransactionAspect<MessageContext>());
            pipe.AddAspect(new LoggingAspect<MessageContext>());
            pipe.AddAspect(new MoveToErrorQueueAspect<MessageContext>());
            pipe.Register(new SendMessage());

            // if destination passed in override the configured bus, assert that the destination is known and send to it
            if (!string.IsNullOrEmpty(destination) && !string.IsNullOrWhiteSpace(destination))
            {
                GuardAgainstUnknownQueue(destination);

                var queue = _writeQueues.First(q => q.FormatName.EndsWith(destination));
                var message = CreateMsmqMessageFromDto(dto);
                var ctx = new MessageContext { Message = message, Config = _config, WriteQueue = queue, ErrorQueue = _errorQueue, OpType = SendOperation, OnStep = LogMessage, OnComplete = SetNextQueueIndex };
                pipe.Invoke(ctx);
                return;
            }

            // if applicable, send to the next queue as we carry out load balancing in a round robin fashion
            if (_config.AutoDistributeOnSend)
            {
                var message = CreateMsmqMessageFromDto(dto);
                var ctx = new MessageContext { Message = message, Config = _config, WriteQueue = _writeQueues.ElementAt(_nextQueue), ErrorQueue = _errorQueue, OpType = SendOperation, OnStep = LogMessage, OnComplete = SetNextQueueIndex };
                pipe.Invoke(ctx);
                return;
            }

            // otherwise send the same message to all defined write queues
            foreach (var writeQueue in _writeQueues)
            {
                var message = CreateMsmqMessageFromDto(dto);
                var ctx = new MessageContext { Message = message, Config = _config, WriteQueue = writeQueue, ErrorQueue = _errorQueue, OpType = SendOperation, OnStep = LogMessage };
                pipe.Invoke(ctx);
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
            GuardAgainstInvalidReadQueue();
            GuardAgainstInvalidErrorQueue();

            // configure the pipeline for receiving messages
            var pipe = new PipeLine<MessageContext>();
            pipe.AddAspect(new FailFastAspect<MessageContext>());
            pipe.AddAspect(new DiscardAspect<MessageContext>());
            pipe.AddAspect(new TransactionAspect<MessageContext>());
            pipe.AddAspect(new LoggingAspect<MessageContext>());
            pipe.AddAspect(new MoveToErrorQueueAspect<MessageContext>());
            pipe.AddAspect(new RemoveFromReadQueueAspect<MessageContext>());
            pipe.AddAspect(new RetryAspect<MessageContext>());
            pipe.Register(new InvokeUserHandlers<T>());

            foreach (Message message in _readQueue.PeekAllMessages())
            {
                var ctx = new MessageContext { Message = message, Config = _config, ReadQueue = _readQueue, ErrorQueue = _errorQueue, Handlers = _handlers, OpType = ReceiveOperation, OnStep = LogMessage };
                pipe.Invoke(ctx);

                if (ctx.FailFast) { break; }
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
            GuardAgainstInvalidReadQueue();
            GuardAgainstInvalidErrorQueue();

            // configure the pipeline for receiving messages
            var pipe = new PipeLine<MessageContext>();
            pipe.AddAspect(new FailFastAspect<MessageContext>());
            pipe.AddAspect(new DiscardAspect<MessageContext>());
            pipe.AddAspect(new TransactionAspect<MessageContext>());
            pipe.AddAspect(new LoggingAspect<MessageContext>());
            pipe.AddAspect(new MoveToErrorQueueAspect<MessageContext>());
            pipe.AddAspect(new RemoveFromReadQueueAspect<MessageContext>());
            pipe.AddAspect(new RetryAspect<MessageContext>());
            pipe.Register(new InvokeUserHandlers<T>());

            _readQueue.ReceiveAsync(message => {

                var ctx = new MessageContext { Message = message, Config = _config, ReadQueue = _readQueue, ErrorQueue = _errorQueue, Handlers = _handlers, OpType = ReceiveOperation, OnStep = LogMessage };
                pipe.Invoke(ctx);

                if (ctx.FailFast)
                {
                    LogMessage("Invoking StopReceiveAsync because FailFast equals true");
                    _readQueue.StopReceiveAsync();
                }
            });

            LogMessage("Receiving...");
        }

        /// <summary>
        /// Read specific message off the given read queue identified by the messageId parameter and copy it to one or more defined write queues
        /// </summary>
        /// <param name="messageId"></param>
        public void Copy(string messageId)
        {
            GuardAgainstInvalidReadQueue();
            GuardAgainstInvalidWriteQueues();

            try
            {
                // configure the pipeline for copying messages
                var pipe = new PipeLine<MessageContext>();
                pipe.AddAspect(new TransactionAspect<MessageContext>());
                pipe.AddAspect(new LoggingAspect<MessageContext>());
                pipe.Register(new CopyMessage());

                var message = _readQueue.PeekMessageBy(messageId);

                foreach (var queue in _writeQueues)
                {
                    var ctx = new MessageContext { Message = message, Config = _config, ReadQueue = _readQueue, WriteQueue = queue, OpType = CopyOperation, OnStep = LogMessage };
                    pipe.Invoke(ctx);
                }
            }
            catch (Exception ex)
            {
                throw new BusException($"A problem occurred copying message: {messageId} - error: {ex}");
            }
        }

        /// <summary>
        /// Read specific message off the given read queue identified by the messageId parameter and do nothing, in effect deleting it
        /// </summary>
        /// <param name="messageId"></param>
        public void Delete(string messageId)
        {
            GuardAgainstInvalidReadQueue();

            try
            {
                _readQueue.ReceiveById(messageId, MessageQueueTransactionType.Single);
            }
            catch (Exception ex)
            {
                throw new BusException($"A problem occurred deleting message: {messageId} - error: {ex}");
            }
        }

        /// <summary>
        /// Peek specific message off the given read queue identified by the messageId parameter and log it out 
        /// </summary>
        /// <param name="messageId"></param>
        public void ViewMessageBody(string messageId)
        {
            GuardAgainstInvalidReadQueue();

            try
            {
                var pipe = new PipeLine<MessageContext>();
                pipe.AddAspect(new TransactionAspect<MessageContext>());
                pipe.AddAspect(new LoggingAspect<MessageContext>());
                pipe.Register(new ViewMessage());

                var message = _readQueue.PeekMessageBy(messageId);

                var ctx = new MessageContext { Message = message, Config = _config, ReadQueue = _readQueue, OpType = CopyOperation, OnStep = LogMessage };

                ctx.Message.Formatter = new BodyAsStringFormatter();
                pipe.Invoke(ctx);
            }
            catch (Exception ex)
            {
                throw new BusException($"A problem occurred viewing message: {messageId} - error: {ex}");
            }
        }

        /// <summary>
        /// Read specific message off the defined error queue and move it to the user defined read queue
        /// </summary>
        public void ReturnErrorMessage(string id)
        {
            GuardAgainstInvalidReadQueue();
            GuardAgainstInvalidErrorQueue();

            try
            {
                // configure the pipeline for return a message to its original queue
                var pipe = new PipeLine<MessageContext>();
                pipe.AddAspect(new TransactionAspect<MessageContext>());
                pipe.AddAspect(new LoggingAspect<MessageContext>());
                pipe.Register(new ReturnToSource());

                Message message = _errorQueue.PeekMessageBy(id);
                var ctx = new MessageContext { Message = message, Config = _config, ReadQueue = _readQueue, ErrorQueue = _errorQueue, OpType = ReturnOperation, OnStep = LogMessage };
                pipe.Invoke(ctx);
            }
            catch (Exception)
            {
                throw new BusException($"Message with id {id} was not found on the error queue");
            }
        }

        /// <summary>
        /// Read messages containing the given type T off the defined
        /// error queue and moves them to the user defined read queue
        /// </summary>
        public void ReturnAllErrorMessages()
        {
            GuardAgainstInvalidReadQueue();
            GuardAgainstInvalidErrorQueue();

            try
            {
                // configure the pipeline for return all message to their original queue
                var pipe = new PipeLine<MessageContext>();
                pipe.AddAspect(new TransactionAspect<MessageContext>());
                pipe.AddAspect(new LoggingAspect<MessageContext>());
                pipe.Register(new ReturnToSource());

                foreach (Message message in _errorQueue.PeekAllMessages())
                {
                    var ctx = new MessageContext { Message = message, Config = _config, ReadQueue = _readQueue, ErrorQueue = _errorQueue, OpType = ReturnOperation, OnStep = LogMessage };
                    pipe.Invoke(ctx);
                }
            }
            catch (Exception ex)
            {
                throw new BusException($"A problem occurred retreiving messages from the error queue: {ex}");
            }
        }

        /// <summary>
        /// Stops listening for messages - only applicable to a bus started with ReceiveAsync
        /// </summary>
        public void StopReceiving()
        {
            _readQueue.StopReceiveAsync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        Message CreateMsmqMessageFromDto<T>(T dto)
        {
            return new Message
            {
                UseAuthentication = false,
                Recoverable = true,
                Body = dto,
                AcknowledgeType = AcknowledgeTypes.FullReachQueue | AcknowledgeTypes.FullReceive,
                UseJournalQueue = _config.UseJournalQueue,
                UseDeadLetterQueue = _config.UseDeadLetterQueue,
                TimeToBeReceived = _config.TimeToBeReceived == TimeSpan.Zero ? MessageQueue.InfiniteTimeout : _config.TimeToBeReceived,
                Formatter = _config.JsonSerialization ? (IMessageFormatter)new JsonFormatter<T>() : new XmlMessageFormatter(new[] { typeof(T) }),
                Label = Guid.NewGuid().ToString(),
            };
        }
        
        ~Bus()
        {
            Dispose(false);
        }

        void GuardAgainstInvalidWriteQueues()
        {
            if (!_writeQueues.Any())
            {
                throw new BusException("Bus has not been configured correctly for sending messages. Did you forget to call DefineWriteQueue on BusBuilder?");
            }
        }

        void GuardAgainstInvalidReadQueue()
        {
            if (_readQueue == null || !_readQueue.IsInitialized)
            {
                throw new BusException("Bus has not been configured correctly for receiving messages. Did you forget to call DefineReadQueue on BusBuilder?");
            }
        }

        void GuardAgainstInvalidErrorQueue()
        {
            if (_errorQueue == null || !_errorQueue.IsInitialized)
            {
                throw new BusException("Bus has not been configured correctly - An error queue has not been defined. Did you forget to call DeineErrorQueue on BusBuilder?");
            }
        }

        void GuardAgainstUnknownQueue(string destination)
        {
            bool exists = _writeQueues.Any(q => q.FormatName.EndsWith(destination));
            if (!exists)
            {
                throw new BusException($"destination: '{destination}' must be in the list of queues defined by the BusBuilder config via WriteQueue or WriteQueues");
            }
        }

        void Dispose(bool disposing)
        {
            if (_disposed) {return;}

            if (disposing)
            {
                _readQueue?.Dispose();
                _errorQueue?.Dispose();
                _writeQueues.ToList().ForEach(q => q.Dispose());
            }

            _disposed = true;
        }

        void SetNextQueueIndex()
        {
            // when AutoDistributeOnSend is configured keep track of the next queue to send a message to. When the last queue is used go back to the start.
            _nextQueue = _nextQueue < _writeQueues.Count() - 1 ? _nextQueue + 1 : 0;
        }

        void LogMessage(string text)
        {
            _logger.Log(text);
        }

        string stripRemoteFrom(string destination)
        {
            string result = destination;

            // remove remote machine part if any
            if (destination.Contains("@"))
            {
                int index = destination.IndexOf("@");
                result = destination.Substring(0, index);
            }

            return result;
        }

        readonly IMessageQueue _readQueue;
        readonly IMessageQueue _errorQueue;
        readonly IEnumerable<IMessageQueue> _writeQueues;
        readonly IBusConfig _config;
        readonly ILogMessages _logger;

        const string SendOperation = "SEND";
        const string ReceiveOperation = "RECEIVE";
        const string ReturnOperation = "RETURN_TO_SOURCE";
        const string CopyOperation = "COPY";
        readonly List<Delegate> _handlers = new List<Delegate>();

        int _nextQueue;
        bool _disposed;
    }
}
