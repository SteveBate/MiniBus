﻿using System;
using System.Collections.Generic;
using MSMQ.Messaging;
using NUnit.Framework;
using MiniBus.Tests.Fakes;

namespace MiniBus.Tests
{
    public abstract class Setup
    {

        [SetUp]
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

        internal FakeValidMessageQueue QueueWithOneMessage(string queueName)
        {
            var queue = new FakeValidMessageQueue(queueName);
            queue.Add(new Message { Body = new FakeDto(), Label = "00000-00000-00000-00000\0000" });
            return queue;
        }

        internal FakeValidMessageQueue QueueWithTwoMessages(string queueName)
        {
            var queue = new FakeValidMessageQueue(queueName);
            queue.Add(new Message { Body = new FakeDto(), Label = "00000-00000-00000-00000\0000" });
            queue.Add(new Message { Body = new FakeDto(), Label = "00000-00000-00000-00000\0001" });
            return queue;
        }
    }
}
