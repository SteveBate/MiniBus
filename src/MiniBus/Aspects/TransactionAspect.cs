using System;
using System.Transactions;
using MiniBus.Core;

namespace MiniBus.Aspects
{
    internal class TransactionAspect<T> : IAspect<T>, IFilter<T> where T : MessageContext
    {
        public void Execute(T ctx)
        {
            ctx.OnStep($"Message: {ctx.Message.Label} - Transaction started");

            using (var scope = new TransactionScope(TransactionScopeOption.Required))
            {
                try
                {
                    Next.Execute(ctx);
                    ctx.OnStep($"Message: {ctx.Message.Label} - Transaction committed" + Environment.NewLine);
                    scope.Complete();
                }
                catch (Exception)
                {
                    if (!ctx.Handled)
                    {
                        ctx.OnStep($"Message: {ctx.Message.Label} - Transaction rolled back" + Environment.NewLine);
                        throw;
                    }
                }
            }
        }

        public IAspect<T> Next { get; set; }
    }
}
