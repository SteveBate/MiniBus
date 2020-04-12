using System;
using MiniBus.Core;

namespace MiniBus.Filters
{
    internal class ViewMessage : IFilter<MessageContext>
    {
        private readonly Action<string> _output;

        public ViewMessage(Action<string> output)
        {
            _output = output;
        }

        public void Execute(MessageContext ctx)
        {
            var payload = (string)ctx.Message.Body;

            var stripped = payload;

            if (stripped.StartsWith("\""))
            {
                stripped = stripped.Substring(0, payload.Length - 1);
            }

            if (stripped.Contains("\"Payload\":"))
            {
                stripped = stripped.Replace("{\"Payload\":", "");
                stripped = stripped.Substring(0, stripped.Length - 1);
            }

            if (stripped.Contains("xml"))
            {
                stripped = stripped.StartsWith("\"")
                    ? stripped.Substring(1, stripped.Length - 2)
                    : stripped.Substring(0, stripped.Length - 1);
            }

            ctx.OnStep($"Message: {ctx.Message.Label} - peeked from queue: {ctx.ReadQueue.FormatName}");

            _output(stripped);
        }
    }
}