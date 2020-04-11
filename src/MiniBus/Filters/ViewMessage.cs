using MiniBus.Core;

namespace MiniBus.Filters
{
    internal class ViewMessage : IFilter<MessageContext>
    {
        public void Execute(MessageContext ctx)
        {
            var payload = (string)ctx.Message.Body;
            var stripped = payload.Substring(0, payload.Length - 1).Replace("{\"Payload\":", "");
            if (stripped.Contains("xml"))
            {
                stripped = stripped.Substring(1, stripped.Length - 2);
            }
            ctx.OnStep($"Message: {ctx.Message.Label} - peeked from queue: {ctx.ReadQueue.FormatName}");
            ctx.OnStep("\n");
            ctx.OnStep($"{stripped}");
            ctx.OnStep("\n");
        }
    }
}