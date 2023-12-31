using MSMQ.Messaging;
using MiniBus.Core;

namespace MiniBus.Filters
{
    internal class SendMessage : IFilter<MessageContext>
    {
        public void Execute(MessageContext ctx)
        {
            ctx.OnStep($"Message: {ctx.Message.Label} - Payload: {ctx.Message.Body}");
            ctx.WriteQueue.Send(ctx.Message, ctx.Message.Label, ctx.Config.EnlistInAmbientTransactions ? MessageQueueTransactionType.Automatic : MessageQueueTransactionType.Single);
            ctx.OnStep($"Message: {ctx.Message.Label} - Sent to queue: {ctx.WriteQueue.FormatName}");
        }
    }
}