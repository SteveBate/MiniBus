using System;
using System.Messaging;
using MiniBus.Contracts;
using System.Threading;

namespace MiniBus.Aspects
{
    internal class MoveToErrorQueueAspect : IHandleMessage<Message>
    {
        public MoveToErrorQueueAspect(IHandleMessage<Message> action, IReadMessageContext context, IBusConfig config, ILogMessages logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
            _inner = action;
        }

        public void Handle(Message msg)
        {
            try
            {
                _inner.Handle(msg);
            }
            catch (Exception ex)
            {
                if (!_config.FailFast)
                {
                    _logger.Log(string.Format("Message: {0} - Moving to error queue: {1}", msg.Label, _context.ErrorQueueName));
                    _context.ErrorQueue.Send(msg, msg.Label, MessageQueueTransactionType.Single);
                    _config.ErrorActions.ForEach(a => ThreadPool.QueueUserWorkItem(cb => a(ex.Message)));
                }

                throw;
            }
        }

        readonly IReadMessageContext _context;
        readonly IBusConfig _config;
        readonly ILogMessages _logger;
        readonly IHandleMessage<Message> _inner;
    }
}
