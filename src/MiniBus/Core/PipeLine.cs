using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniBus.Core
{
    public class PipeLine<T> where T : BaseMessage
    {
        /// <summary>
        /// Constructor initializes the pipeline creating a default aspect as the context in which registered filters are executed
        /// </summary>
        public PipeLine()
        {
            _aspects.Add(new DefaultAspect(Execute));
        }

        /// AddAspect provides the means to supply cross cutting concerns around the actual unit of work.        
        public void AddAspect(IAspect<T> aspect)
        {
            _aspects.Insert(_aspects.Count - 1, aspect);
            _aspects.Aggregate((a, b) => a.Next = b);
        }

        /// Register keeps track of the individual steps that make up the pipeline
        public void Register(IFilter<T> filter)
        {
            _filters.Add(filter);
        }

        /// Finally registers filters that must always run even when an error occurs
        public void Finally(IFilter<T> filter)
        {
            _finallyFilters.Add(filter);
        }

        /// Invoke kicks off the unit of work including all registered aspects
        public void Invoke(T msg)
        {
            _aspects.First().Execute(msg);
        }

        /// Execute is where the actual steps/filters are iterated through
        private void Execute(T msg)
        {
            msg.OnStart?.Invoke();
            try
            {
                foreach (var f in _filters)
                {
                    if (msg.Stop) return;
                    f.Execute(msg);
                }
                msg.OnSuccess?.Invoke();
            }
            finally
            {
                foreach (var h in _finallyFilters)
                {
                    h.Execute(msg);
                }
                msg.OnComplete?.Invoke();
            }
        }

        private readonly List<IFilter<T>> _filters = new List<IFilter<T>>();
        private readonly List<IFilter<T>> _finallyFilters = new List<IFilter<T>>();
        private readonly List<IAspect<T>> _aspects = new List<IAspect<T>>();

        /// DefaultAspect is the context that the pipeline's unit of work runs under
        private class DefaultAspect : IAspect<T>, IFilter<T>
        {
            public DefaultAspect(Action<T> action)
            {
                _inner = action;
            }

            public void Execute(T ctx)
            {
                _inner(ctx);
            }

            public IAspect<T> Next { get; set; }

            readonly Action<T> _inner;
        }
    }
}
