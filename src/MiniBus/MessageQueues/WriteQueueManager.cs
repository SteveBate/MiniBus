using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniBus.Contracts;

namespace MiniBus.MessageQueues
{
    internal class WriteQueueManager : IDisposable
    {
        public WriteQueueManager(bool distributeSends, IEnumerable<IMessageQueue> queues)
        {
            _distributeSends = distributeSends;
            _writeQueues = queues;
        }

        public bool HasWriteQueues
        {
            get { return _writeQueues.Count() > 0; }
        }

        public IEnumerable<IMessageQueue> GetWriteQueues()
        {
            if (_distributeSends)
            {
                var nextQueue = new List<IMessageQueue> { _writeQueues.ElementAt(_nextQueueIndex) };
                _nextQueueIndex = _nextQueueIndex < _writeQueues.Count() - 1 ? _nextQueueIndex + 1 : 0;
                return nextQueue;
            }
            else
            {
                return _writeQueues;
            }
        }

        public void Dispose()
        {
            foreach (var writeQueue in _writeQueues)
            {
                if (writeQueue != null)
                {
                    writeQueue.Dispose();
                }
            }
        }

        readonly bool _distributeSends;
        readonly IEnumerable<IMessageQueue> _writeQueues;
        int _nextQueueIndex;
    }
}
