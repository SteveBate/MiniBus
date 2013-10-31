using System;
using System.Collections.Generic;
using System.Messaging;
using MiniBus.Contracts;
using MiniBus.Formatters;

namespace MiniBus.Handlers
{
    internal class ReceiveMessageHandler<T> : IHandleMessage<Message>
    {
        public ReceiveMessageHandler(List<Delegate> handlers, IBusConfig config, ILogMessages logger)
        {
            _handlers = handlers;
            _config = config;
            _logger = logger;            
        }

        public void Handle(Message msg)
        {
            T payload = GetPayload(msg);
            _logger.Log(string.Format("Message: {0} - Payload: {1}", msg.Label, msg.Body));

            _handlers.ForEach(action =>
            {
                _logger.Log(string.Format("Message: {0} - Invoking registered handler", msg.Label));
                ((Action<T>)action)(payload);
            });           
        }    

        T GetPayload(Message msg)
        {
            msg.Formatter = _config.JsonSerialization ? (IMessageFormatter) new JsonFormatter<T>() : new XmlMessageFormatter(new[] {typeof(T)});
            return (T) msg.Body;
        }

        readonly IBusConfig _config;
        readonly ILogMessages _logger;
        readonly List<Delegate> _handlers;  
    }
}