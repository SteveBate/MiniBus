using System;
using System.Linq;
using MSMQ.Messaging;
using MiniBus.Core;
using MiniBus.Formatters;

namespace MiniBus.Filters
{
    /// <summary>
    /// InvokeUserHandlers deserializes the message body from JSON or XML into the user supplied type (T) and invokes the client configured handlers
    /// </summary>
    internal class InvokeUserHandlers<T> : IFilter<MessageContext>
    {
        public void Execute(MessageContext ctx)
        {
            var payload = GetPayload(ctx);
            ctx.OnStep($"Message: {ctx.Message.Label} - Payload: {ctx.Message.Body}");

            // loop through all user registered handlers and invoke them
            ctx.Handlers.ToList().ForEach(action =>
            {
                ctx.OnStep($"Message: {ctx.Message.Label} - Invoking registered handler");                
                ((Action<T>)action)(payload);
            });
        }

        T GetPayload(MessageContext ctx)
        {
            ctx.Message.Formatter = ctx.Config.JsonSerialization ? (IMessageFormatter)new JsonFormatter<T>() : new XmlMessageFormatter(new[] { typeof(T) });
            return (T)ctx.Message.Body;
        }
    }
}
