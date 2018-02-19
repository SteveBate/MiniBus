using MiniBus.Core;

namespace MiniBus.Aspects
{
    /// <summary>
    /// FailFastAspect stops processing of the queues - sometimes preserving message processing order is critical
    /// </summary>
    internal class FailFastAspect<T> : IAspect<T>, IFilter<T> where T : MessageContext
    {
        public void Execute(T ctx)
        {
            try
            {
                Next.Execute(ctx);
            }
            catch
            {
                if (!ctx.Handled)
                {
                    ctx.FailFast = ctx.Config.FailFast;
                    if (ctx.FailFast)
                    {
                        ctx.OnStep($"Message: {ctx.Message.Label} - FailFast option enabled - Queue processing halted");
                    }
                }
            }
        }

        public IAspect<T> Next { get; set; }
    }
}
