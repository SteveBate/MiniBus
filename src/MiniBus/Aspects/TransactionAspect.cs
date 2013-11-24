using System;
using System.Messaging;
using System.Transactions;
using MiniBus.Contracts;

namespace MiniBus.Aspects
{
    internal class TransactionAspect : IHandleMessage<Message>
    {
        public TransactionAspect(IHandleMessage<Message> action, ILogMessages logger)
        {            
            _inner = action;
            _logger = logger;
        }

        public void Handle(Message msg)
        {
            _logger.Log(string.Format("Message: {0} - Transaction started", msg.Label));

            using (var scope = new TransactionScope(TransactionScopeOption.Required, TransactionManager.DefaultTimeout))
            {
                try
                {
                    _inner.Handle(msg);
                    _logger.Log(string.Format("Message: {0} - Transaction committed", msg.Label));
                    scope.Complete();
                }
                catch (Exception)
                {
                    _logger.Log(string.Format("Message: {0} - Transaction rolled back", msg.Label));
                    throw;
                }
            }
        }

        readonly ILogMessages _logger;
        readonly IHandleMessage<Message> _inner;
    }
}