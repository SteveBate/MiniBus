using System;
using System.Threading;
using System.Transactions;
using MiniBus.Core;

namespace MiniBus.Aspects
{
    /// <summary>
    /// RetryAspect - In the event of an error try to process the message again up to the configured number of retries
    /// </summary>
    internal class RetryAspect<T> : IAspect<T>, IFilter<T> where T : MessageContext
    {
        public void Execute(T ctx)
        {
            try
            {
                Next.Execute(ctx);
                ctx.Handled = true;
                _retry = 0;
            }
            catch (Exception e)
            {
                ctx.OnStep($"TRANSACTION STATUS: {Transaction.Current.TransactionInformation.Status} - REASON: {e.Message}");
 
                // once an exception occurs the current transaction is damaged goods
                Transaction.Current.Rollback();

                _retry++;

                if (_retry <= ctx.Config.MaxRetries)
                {
                    if (ctx.Config.SlidingRetryInterval > 0)
                    {
                        // wait for a time specified by SlidingRetryInterval before retrying. Default value is 1 second
                        double wait = _retry * (ctx.Config.SlidingRetryInterval / 1000.00);
                        ctx.OnStep($"Message: {ctx.Message.Label} - Waiting {wait} seconds before attempt {_retry}");
                        Thread.Sleep(ctx.Config.SlidingRetryInterval * _retry);
                    }

                    ctx.OnStep($"Message: {ctx.Message.Label} - Retry attempt {_retry}");

                    // defaults for TransactionScope are Serializable and 1 minute neither of which are ideal for SQL Server
                    var options = new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TransactionManager.MaximumTimeout };
                    
                    // retry in the context of a new transaction
                    using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, options))
                    {
                        Execute(ctx);
                        scope.Complete();
                        ctx.OnStep($"Message: {ctx.Message.Label} - Retry successful");
                    }
                }
                else
                {
                    ctx.OnStep($"Message: {ctx.Message.Label} - Invocation failed");
                    throw;
                }
            }
        }

        public IAspect<T> Next { get; set; }

        int _retry;
    }
}
