using System.Messaging;
using MiniBus.Core;

namespace MiniBus.Filters
{
    internal class ReturnToSource : IFilter<MessageContext>
    {
        public void Execute(MessageContext ctx)
        {
            ctx.OnStep($"Message: {ctx.Message.Label} - Removing from queue: {ctx.ErrorQueue.FormatName}");
            ctx.ErrorQueue.ReceiveById(ctx.Message.Id, MessageQueueTransactionType.Single);
            ctx.OnStep($"Message: {ctx.Message.Label} - Sending to queue: {ctx.ReadQueue.FormatName}");
            ctx.ReadQueue.Send(ctx.Message, ctx.Message.Label, MessageQueueTransactionType.Single);
        }
    }
}
