using MSMQ.Messaging;
using MiniBus.Core;

namespace MiniBus.Filters
{
    internal class CopyMessage : IFilter<MessageContext>
    {
        public void Execute(MessageContext ctx)
        {
            ctx.OnStep($"Message: {ctx.Message.Label} - copied from queue: {ctx.ReadQueue.FormatName}");
            ctx.WriteQueue.Send(ctx.Message, ctx.Message.Label, ctx.Config.EnlistInAmbientTransactions ? MessageQueueTransactionType.Automatic : MessageQueueTransactionType.Single);
            ctx.OnStep($"Message: {ctx.Message.Label} - Sending to queue: {ctx.WriteQueue.FormatName}");
        }
    }
}
