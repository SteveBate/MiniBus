using MiniBus.Contracts;
using MiniBus.Tests.Fakes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Messaging;

namespace MiniBus.Tests
{
    public abstract class Setup
    {

        [TestFixtureSetUp]
        public void Run_once_before_all_tests()
        {
            ThreeHandlers = new List<Delegate>
                {
                    new Action<FakeDto>(new FakeEndUserHandler().Handle),
                    new Action<FakeDto>(new FakeEndUserHandler().Handle),
                    new Action<FakeDto>(new FakeEndUserHandler().Handle)
                };
        }

        protected List<Delegate> ThreeHandlers;

        internal FakeValidMessageQueue QueueWithOneMessage()
        {
            var queue = new FakeValidMessageQueue();
            queue.Add(new Message { Body = new FakeDto(), Label = "00000-00000-00000-00000\0000" });
            return queue;
        }

        internal FakeValidMessageQueue QueueWithTwoMessages()
        {
            var queue = new FakeValidMessageQueue();
            queue.Add(new Message { Body = new FakeDto(), Label = "00000-00000-00000-00000\0000" });
            queue.Add(new Message { Body = new FakeDto(), Label = "00000-00000-00000-00000\0001" });
            return queue;
        }
    }
}
