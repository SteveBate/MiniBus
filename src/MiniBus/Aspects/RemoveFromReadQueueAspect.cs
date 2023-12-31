using MSMQ.Messaging;
using MiniBus.Core;

namespace MiniBus.Aspects
{
    /// <summary>
    /// RemoveFromReadQueueAspect removes the message from the source queue.
    /// However, in the event of an error if FailFast is true the message is left on the read queue
    /// </summary>
    internal class RemoveFromReadQueueAspect<T> : IAspect<T>, IFilter<T> where T : MessageContext
    {
        public void Execute(T ctx)
        {
            try
            {
                Next.Execute(ctx);
                ctx.OnStep($"Message: {ctx.Message.Label} - Removing from read queue: {ctx.ReadQueue.FormatName}");
                ctx.ReadQueue.ReceiveById(ctx.Message.Id, MessageQueueTransactionType.Single);
            }
            catch
            {
                if (!ctx.Config.FailFast && !ctx.Handled)
                {
                    ctx.OnStep($"Message: {ctx.Message.Label} - Removing from read queue: {ctx.ReadQueue.FormatName}");
                    ctx.ReadQueue.ReceiveById(ctx.Message.Id, MessageQueueTransactionType.Single);
                }

                throw;
            }
        }

        public IAspect<T> Next { get; set; }
    }

}
