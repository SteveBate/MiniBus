using System;
using System.Threading;
using MSMQ.Messaging;
using MiniBus.Core;

namespace MiniBus.Aspects
{
    internal class MoveToErrorQueueAspect<T> : IAspect<T>, IFilter<T> where T : MessageContext
    {
        public void Execute(T ctx)
        {
            try
            {
                Next.Execute(ctx);
            }
            catch (Exception ex)
            {
                if (!ctx.Config.FailFast && !ctx.Config.DiscardFailures && !ctx.Handled)
                {
                    ctx.OnStep($"Message: {ctx.Message.Label} - Moving to error queue: {ctx.ErrorQueue.FormatName}");
                    ctx.ErrorQueue.Send(ctx.Message, ctx.Message.Label, MessageQueueTransactionType.Single);
                    ctx.Config.ErrorActions?.ForEach(a => ThreadPool.QueueUserWorkItem(cb => a(ex.Message)));
                }

                throw;
            }
        }

        public IAspect<T> Next { get; set; }
    }
}
