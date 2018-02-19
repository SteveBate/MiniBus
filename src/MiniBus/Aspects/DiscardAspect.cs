using System;
using MiniBus.Core;

namespace MiniBus.Aspects
{
    /// <summary>
    /// DiscardAspect throws the message away
    /// </summary>
    internal class DiscardAspect<T> : IAspect<T>, IFilter<T> where T : MessageContext
    {
        public void Execute(T ctx)
        {
            try
            {
                Next.Execute(ctx);
            }
            catch (Exception)
            {
                if (ctx.Config.DiscardFailures && !ctx.Handled)
                {
                    ctx.OnStep($"Message: {ctx.Message.Label} - DiscardFailures option enabled - Payload discarded");
                }

                throw;
            }
        }

        public IAspect<T> Next { get; set; }
    }
}
