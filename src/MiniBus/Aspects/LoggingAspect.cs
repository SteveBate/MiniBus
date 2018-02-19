using System;
using MiniBus.Core;

namespace MiniBus.Aspects
{
    internal class LoggingAspect<T> : IAspect<T>, IFilter<T> where T : MessageContext
    {
        public void Execute(T ctx)
        {
            ctx.OnStep($"Message: {ctx.Message.Label} - Started {ctx.OpType} Operation");
            try
            {
                Next.Execute(ctx);
            }
            catch (Exception ex)
            {
                if (!ctx.Handled)
                {
                    ctx.OnStep($"Message: {ctx.Message.Label} - EXCEPTION - {ex.Message}");
                    ctx.OnStep($"Message: {ctx.Message.Label} - {ex}");
                    throw;
                }
            }
            finally
            {
                ctx.OnStep($"Message: {ctx.Message.Label} - Completed {ctx.OpType} Operation");
            }
        }

        public IAspect<T> Next { get; set; }
    }
}
