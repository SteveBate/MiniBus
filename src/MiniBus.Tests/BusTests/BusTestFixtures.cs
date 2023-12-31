using System;
using System.Linq;
using MiniBus.Exceptions;
using MiniBus.Logging;
using MiniBus.Tests.Fakes;
using NUnit.Framework;

namespace MiniBus.Tests.BusTests
{
    [TestFixture]
    public class When_sending_message
    {
        [Test]
        public void Should_place_on_a_single_queue()
        {
            var msg = new FakeDto();
            var writeQueues = new[] { new FakeValidMessageQueue("writeQueue1") };
            var bus = new Bus(new FakeBusConfig(), new NullLogger(), new FakeValidMessageQueue("errorQueue"), new FakeValidMessageQueue("readQueue"), writeQueues);

            bus.Send(msg);

            Assert.That(writeQueues[0].Count, Is.EqualTo(1));
        }

        [Test]
        public void Should_place_on_all_write_queues_by_default()
        {
            var msg = new FakeDto();
            var writeQueues = new[] { new FakeValidMessageQueue("writeQueue1"), new FakeValidMessageQueue("writeQueue2"), new FakeValidMessageQueue("writeQueue3") };
            var bus = new Bus(new FakeBusConfig(), new NullLogger(), new FakeValidMessageQueue("errorQueue"), new FakeValidMessageQueue("readQueue"), writeQueues);

            bus.Send(msg);
            
            Assert.That(writeQueues[0].Count, Is.EqualTo(1));
            Assert.That(writeQueues[1].Count, Is.EqualTo(1));
            Assert.That(writeQueues[2].Count, Is.EqualTo(1));
        }

        [Test]
        public void Should_place_only_on_first_write_queue_if_auto_distribute_configured()
        {
            var msg = new FakeDto();
            var writeQueues = new[] { new FakeValidMessageQueue("writeQueue1"), new FakeValidMessageQueue("writeQueue2"), new FakeValidMessageQueue("writeQueue3") };
            var bus = new Bus(new FakeBusConfig { AutoDistributeOnSend = true }, new NullLogger(), new FakeValidMessageQueue("errorQueue"), new FakeValidMessageQueue("readQueue"), writeQueues);

            bus.Send(msg);

            Assert.That(writeQueues[0].Count, Is.EqualTo(1));
            Assert.That(writeQueues[1].Count, Is.EqualTo(0));
            Assert.That(writeQueues[2].Count, Is.EqualTo(0));
        }

        [Test]
        public void Should_place_on_next_write_queue_if_auto_distribute_configured()
        {
            var msg = new FakeDto();
            var writeQueues = new[] { new FakeValidMessageQueue("writeQueue1"), new FakeValidMessageQueue("writeQueue2"), new FakeValidMessageQueue("writeQueue3") };
            var bus = new Bus(new FakeBusConfig { AutoDistributeOnSend = true }, new NullLogger(), new FakeValidMessageQueue("errorQueue"), new FakeValidMessageQueue("readQueue"), writeQueues);

            bus.Send(msg);

            Assert.That(writeQueues[0].Count, Is.EqualTo(1));
            Assert.That(writeQueues[1].Count, Is.EqualTo(0));
            Assert.That(writeQueues[2].Count, Is.EqualTo(0));

            bus.Send(msg);

            Assert.That(writeQueues[0].Count, Is.EqualTo(1));
            Assert.That(writeQueues[1].Count, Is.EqualTo(1));
            Assert.That(writeQueues[2].Count, Is.EqualTo(0));

            bus.Send(msg);

            Assert.That(writeQueues[0].Count, Is.EqualTo(1));
            Assert.That(writeQueues[1].Count, Is.EqualTo(1));
            Assert.That(writeQueues[2].Count, Is.EqualTo(1));
        }

        [Test]
        public void Should_place_on_requested_queue_when_destination_passed_in_and_queue_is_known()
        {
            var msg = new FakeDto();
            var writeQueues = new[] { new FakeValidMessageQueue("writeQueue1"), new FakeValidMessageQueue("writeQueue2"), new FakeValidMessageQueue("writeQueue3") };
            var bus = new Bus(new FakeBusConfig(), new NullLogger(), new FakeValidMessageQueue("errorQueue"), new FakeValidMessageQueue("readQueue"), writeQueues);

            bus.Send(msg, "writeQueue2");

            Assert.That(writeQueues[0].Count, Is.EqualTo(0));
            Assert.That(writeQueues[1].Count, Is.EqualTo(1));
            Assert.That(writeQueues[2].Count, Is.EqualTo(0));
        }

        [Test]
        public void Should_throw_when_destination_passed_in_and_queue_is_not_known()
        {
            var msg = new FakeDto();
            var writeQueues = new[] { new FakeValidMessageQueue("writeQueue1"), new FakeValidMessageQueue("writeQueue2"), new FakeValidMessageQueue("writeQueue3") };
            var bus = new Bus(new FakeBusConfig(), new NullLogger(), new FakeValidMessageQueue("errorQueue"), new FakeValidMessageQueue("readQueue"), writeQueues);

            var exception = Assert.Throws<BusException>(() => bus.Send(msg, "unknownQueue"));

            Assert.That(exception.Message, Is.EqualTo("destination: 'unknownQueue' must be in the list of queues defined by the BusBuilder config via WriteQueue or WriteQueues"));
        }

        [Test]
        public void Should_log_all_steps_involved()
        {
            var logger = new FakeLogger();
            var msg = new FakeDto();
            var writeQueues = new[] { new FakeValidMessageQueue("writeQueue1") };
            var bus = new Bus(new FakeBusConfig(), logger, new FakeValidMessageQueue("errorQueue"), new FakeValidMessageQueue("readQueue"), writeQueues);

            bus.Send(msg);

            Assert.That(logger[0], Does.Contain("Transaction started"));
            Assert.That(logger[1], Does.Contain("Started SEND Operation"));
            Assert.That(logger[2], Does.Contain("Payload: FakeDto"));
            Assert.That(logger[3], Does.Contain("Sent to queue: writeQueue1"));
            Assert.That(logger[4], Does.Contain("Completed SEND Operation"));
            Assert.That(logger[5], Does.Contain("Transaction committed"));
        }

        [Test]
        public void Should_throw_when_write_queues_are_undefined()
        {
            var emptyWriteQueueList = new FakeValidMessageQueue[] { };
            var bus = new Bus(new BusConfig(), new NullLogger(), new FakeValidMessageQueue("errorQueue"), new FakeValidMessageQueue("readQueue"), emptyWriteQueueList);

            var exception = Assert.Throws<BusException>(() => bus.Send(new FakeDto()));

            Assert.That(exception.Message, Is.EqualTo("Bus has not been configured correctly for sending messages. Did you forget to call DefineWriteQueue on BusBuilder?"));
        }

        [Test]
        public void Should_throw_when_error_queue_is_undefined()
        {
            var bus = new Bus(new BusConfig(), new NullLogger(), null, new FakeValidMessageQueue("readQueue"), new[] { new FakeValidMessageQueue("writeQueue1") });

            var exception = Assert.Throws<BusException>(bus.ReturnAllErrorMessages);

            Assert.That(exception.Message, Is.EqualTo("Bus has not been configured correctly - An error queue has not been defined. Did you forget to call DeineErrorQueue on BusBuilder?"));
        }
    }

    [TestFixture]
    public class When_returning_error_messages : Setup
    {
        [Test]
        public void Should_move_all_to_read_queue()
        {
            var errorQueue = QueueWithTwoMessages("errorQueue");
            var readQueue = new FakeValidMessageQueue("readQueue");
            var bus = new Bus(new FakeBusConfig(), new NullLogger(), errorQueue, readQueue, new[] { new FakeValidMessageQueue("writeQueue1") });

            bus.ReturnAllErrorMessages();

            Assert.That(errorQueue.Count, Is.EqualTo(0));
            Assert.That(readQueue.Count, Is.EqualTo(2));
        }

        [Test]
        public void Should_move_specific_message_to_read_queue()
        {
            var errorQueue = QueueWithTwoMessages("errorQueue");
            var readQueue = new FakeValidMessageQueue("readQueue");
            var bus = new Bus(new FakeBusConfig(), new NullLogger(), errorQueue, readQueue, new[] { new FakeValidMessageQueue("writeQueue1") });

            bus.ReturnErrorMessage("00000-00000-00000-00000\0000");

            Assert.That(errorQueue.Count, Is.EqualTo(1));
            Assert.That(readQueue.Count, Is.EqualTo(1));
        }

        [Test]
        public void Should_throw_when_specific_message_is_not_found()
        {
            var errorQueue = QueueWithTwoMessages("errorQueue");
            var readQueue = new FakeValidMessageQueue("readQueue");
            var bus = new Bus(new FakeBusConfig(), new NullLogger(), errorQueue, readQueue, new[] { new FakeValidMessageQueue("writeQueue1") });

            var exception = Assert.Throws<BusException>(() => bus.ReturnErrorMessage("0"));

            Assert.That(exception.Message, Is.EqualTo("Message with id 0 was not found on the error queue"));
        }

        [Test]
        public void Should_throw_when_read_queue_is_undefined()
        {
            var bus = new Bus(new BusConfig(), new NullLogger(), new FakeValidMessageQueue("errowQueue"), new FakeInvalidMessageQueue(), new[] { new FakeValidMessageQueue("writeQueue1") });

            var exception = Assert.Throws<BusException>(bus.ReturnAllErrorMessages);

            Assert.That(exception.Message, Is.EqualTo("Bus has not been configured correctly for receiving messages. Did you forget to call DefineReadQueue on BusBuilder?"));
        }

        [Test]
        public void Should_log_all_steps_involved()
        {
            var logger = new FakeLogger();
            var readQueue = new FakeValidMessageQueue("readQueue");
            var bus = new Bus(new FakeBusConfig(), logger, QueueWithOneMessage("errorQueue"), readQueue, new[] { new FakeValidMessageQueue("writeQueue1") });

            bus.ReturnAllErrorMessages();

            Assert.That(logger[0], Does.Contain("Transaction started"));
            Assert.That(logger[1], Does.Contain("Started RETURN_TO_SOURCE Operation"));
            Assert.That(logger[2], Does.Contain("Removing from queue: errorQueue"));
            Assert.That(logger[3], Does.Contain("Sending to queue: readQueue"));
            Assert.That(logger[4], Does.Contain("Completed RETURN_TO_SOURCE Operation"));
            Assert.That(logger[5], Does.Contain("Transaction committed"));
        }
        
        [Test]
        public void Should_throw_when_error_queue_is_undefined()
        {
            var bus = new Bus(new BusConfig(), new NullLogger(), null, new FakeValidMessageQueue("readQueue"), new[] { new FakeValidMessageQueue("writeQueue1") });

            var exception = Assert.Throws<BusException>(bus.ReturnAllErrorMessages);

            Assert.That(exception.Message, Is.EqualTo("Bus has not been configured correctly - An error queue has not been defined. Did you forget to call DeineErrorQueue on BusBuilder?"));
        }
    }

    [TestFixture]
    public class When_copying_messages : Setup
    {
        [Test]
        public void Should_copy_specific_message_to_write_queue()
        {
            var readQueue = QueueWithOneMessage("readQueue");
            var writeQueues = new[] { new FakeValidMessageQueue("writeQueue1") };
            var bus = new Bus(new FakeBusConfig(), new NullLogger(), null, readQueue, writeQueues);

            bus.Copy("00000-00000-00000-00000\0000");

            Assert.That(readQueue.Count, Is.EqualTo(1));
            Assert.That(writeQueues.ElementAt(0).Count, Is.EqualTo(1));
        }

        [Test]
        public void Should_copy_specific_message_to_all_defined_write_queuse()
        {
            var readQueue = QueueWithOneMessage("readQueue");
            var writeQueues = new[] { new FakeValidMessageQueue("writeQueue1"), new FakeValidMessageQueue("writeQueue2"), new FakeValidMessageQueue("writeQueue3") };
            var bus = new Bus(new FakeBusConfig(), new NullLogger(), null, readQueue, writeQueues);

            bus.Copy("00000-00000-00000-00000\0000");

            Assert.That(readQueue.Count, Is.EqualTo(1));
            Assert.That(writeQueues.ElementAt(0).Count, Is.EqualTo(1));
            Assert.That(writeQueues.ElementAt(1).Count, Is.EqualTo(1));
            Assert.That(writeQueues.ElementAt(2).Count, Is.EqualTo(1));
        }

        [Test]
        public void Should_log_all_steps_involved()
        {
            var logger = new FakeLogger();
            var readQueue = QueueWithOneMessage("readQueue");
            var writeQueues = new[] { new FakeValidMessageQueue("writeQueue1") };
            var bus = new Bus(new FakeBusConfig(), logger, null, readQueue, writeQueues);

            bus.Copy("00000-00000-00000-00000\0000");

            Assert.That(logger[0], Does.Contain("Transaction started"));
            Assert.That(logger[1], Does.Contain("Started COPY Operation"));
            Assert.That(logger[2], Does.Contain("copied from queue: readQueue"));
            Assert.That(logger[3], Does.Contain("Sending to queue: writeQueue1"));
            Assert.That(logger[4], Does.Contain("Completed COPY Operation"));
            Assert.That(logger[5], Does.Contain("Transaction committed"));
        }
    }

    [TestFixture]
    public class When_receiving_message : Setup
    {
        [Test]
        public void Should_invoke_registered_handler()
        {
            var handler = new FakeEndUserHandler();
            var bus = new Bus(new FakeBusConfig(), new NullLogger(), new FakeValidMessageQueue("errorQueue"), QueueWithOneMessage("readQueue"), new[] { new FakeValidMessageQueue("writeQueue") });
            bus.RegisterHandler(handler);

            bus.Receive<FakeDto>();

            Assert.That(handler.InvokeCount, Is.EqualTo(1));
        }

        [Test]
        public void Should_invoke_all_registered_handlers()
        {
            var bus = new Bus(new FakeBusConfig(), new NullLogger(), new FakeValidMessageQueue("errorQueue"), QueueWithOneMessage("readQueue"), new[] { new FakeValidMessageQueue("writeQueue") });
            var handler = new FakeEndUserHandler();
            bus.RegisterHandler(handler);
            bus.RegisterHandler(handler);
            bus.RegisterHandler(handler);

            bus.Receive<FakeDto>();

            Assert.That(handler.InvokeCount, Is.EqualTo(3));
        }

        [Test]
        public void Should_log_all_steps_involved()
        {
            var logger = new FakeLogger();
            var handler = new FakeEndUserHandler();
            var bus = new Bus(new FakeBusConfig(), logger, new FakeValidMessageQueue("errorQueue"), QueueWithOneMessage("readQueue"), new[] { new FakeValidMessageQueue("writeQueue") });
            bus.RegisterHandler(handler);

            bus.Receive<FakeDto>();

            Assert.That(logger[0], Does.Contain("Transaction started"));
            Assert.That(logger[1], Does.Contain("Started RECEIVE Operation"));
            Assert.That(logger[2], Does.Contain("Payload: FakeDto"));
            Assert.That(logger[3], Does.Contain("Invoking registered handler"));
            Assert.That(logger[4], Does.Contain("Removing from read queue: readQueue"));
            Assert.That(logger[5], Does.Contain("Completed RECEIVE Operation"));
            Assert.That(logger[6], Does.Contain("Transaction committed"));
        }

        [Test]
        public void Should_throw_when_read_queue_is_undefined()
        {
            var bus = new Bus(new BusConfig(), new NullLogger(), new FakeValidMessageQueue("errorQueue"), new FakeInvalidMessageQueue(), new[] { new FakeValidMessageQueue("writeQueue1") });

            var exception = Assert.Throws<BusException>(bus.Receive<FakeDto>);

            Assert.That(exception.Message, Is.EqualTo("Bus has not been configured correctly for receiving messages. Did you forget to call DefineReadQueue on BusBuilder?"));
        }

        [Test]
        public void Should_move_failed_messages_to_error_queue_when_fail_fast_not_configured()
        {
            var logger = new FakeLogger();
            var handler = new FakeExceptionThrowingUserHandler();
            var errorQueue = new FakeValidMessageQueue("errorQueue");
            var readQueue = QueueWithTwoMessages("readQueue");
            var bus = new Bus(new FakeBusConfig { FailFast = false }, logger, errorQueue, readQueue, new[] { new FakeValidMessageQueue("writeQueue1") });
            bus.RegisterHandler(handler);

            bus.Receive<FakeDto>();

            Assert.That(errorQueue.Count, Is.EqualTo(2));
            Assert.That(readQueue.Count, Is.EqualTo(0));
        }

        [Test]
        public void Should_leave_failed_message_on_read_queue_when_fail_fast_configured()
        {
            var logger = new FakeLogger();
            var handler = new FakeExceptionThrowingUserHandler();
            var errorQueue = new FakeValidMessageQueue("errorQueue");
            var readQueue = QueueWithTwoMessages("readQueue");
            var bus = new Bus(new FakeBusConfig { FailFast = true }, logger, errorQueue, readQueue, new[] { new FakeValidMessageQueue("writeQueue1") });
            bus.RegisterHandler(handler);

            bus.Receive<FakeDto>();

            Assert.That(errorQueue.Count, Is.EqualTo(0));
            Assert.That(readQueue.Count, Is.EqualTo(2));
            Assert.That(logger[10], Does.Contain("FailFast option enabled - Queue processing halted"));
        }

        [Test]
        public void Should_discard_failed_messages_to_when_discard_failures_configured()
        {
            var logger = new FakeLogger();
            var handler = new FakeExceptionThrowingUserHandler();
            var errorQueue = new FakeValidMessageQueue("errorQueue");
            var readQueue = QueueWithTwoMessages("readQueue");
            var bus = new Bus(new FakeBusConfig { DiscardFailures = true }, logger, errorQueue, readQueue, new[] { new FakeValidMessageQueue("writeQueue1") });
            bus.RegisterHandler(handler);

            bus.Receive<FakeDto>();

            Assert.That(errorQueue.Count, Is.EqualTo(0));
            Assert.That(readQueue.Count, Is.EqualTo(0));
            Assert.That(logger[11], Does.Contain("DiscardFailures option enabled - Payload discarded"));
            Assert.That(logger[23], Does.Contain("DiscardFailures option enabled - Payload discarded"));
        }

        [Test]
        public void Should_always_retry_when_not_configured_for_environmental_errors_only()
        {
            var logger = new FakeLogger();
            var handler = new FakeExceptionThrowingUserHandler();
            var errorQueue = new FakeValidMessageQueue("errorQueue");
            var bus = new Bus(new FakeBusConfig { MaxRetries = 2 }, logger, errorQueue, QueueWithOneMessage("readQueue"), new[] { new FakeValidMessageQueue("writeQueue1") });
            bus.RegisterHandler(handler);

            bus.Receive<FakeDto>();

            Assert.That(logger[0], Does.Contain("Transaction started"));
            Assert.That(logger[1], Does.Contain("Started RECEIVE Operation"));
            Assert.That(logger[2], Does.Contain("Payload: FakeDto"));
            Assert.That(logger[3], Does.Contain("Invoking registered handler"));
            Assert.That(logger[4], Does.Contain("TRANSACTION STATUS: Active - REASON: The method or operation is not implemented."));
            Assert.That(logger[5], Does.Contain("Retry attempt 1"));
            Assert.That(logger[6], Does.Contain("Payload: FakeDto"));
            Assert.That(logger[7], Does.Contain("Invoking registered handler"));
            Assert.That(logger[8], Does.Contain("TRANSACTION STATUS: Active - REASON: The method or operation is not implemented."));
            Assert.That(logger[9], Does.Contain("Retry attempt 2"));
            Assert.That(logger[10], Does.Contain("Payload: FakeDto"));
            Assert.That(logger[11], Does.Contain("Invoking registered handler"));
            Assert.That(logger[12], Does.Contain("TRANSACTION STATUS: Active - REASON: The method or operation is not implemented."));
            Assert.That(logger[13], Does.Contain("Invocation failed"));
            Assert.That(logger[14], Does.Contain("Removing from read queue: readQueue"));
            Assert.That(logger[15], Does.Contain("Moving to error queue: errorQueue"));
            Assert.That(logger[16], Does.Contain("EXCEPTION - The method or operation is not implemented."));
            Assert.That(logger[18], Does.Contain("Completed RECEIVE Operation"));
            Assert.That(logger[19], Does.Contain("Transaction rolled back"));
        }

        [Test]
        public void Should_not_retry_when_configured_for_environmental_errors_only_and_error_is_user_error()
        {
            var logger = new FakeLogger();
            var handler = new FakeExceptionThrowingUserHandler();
            var errorQueue = new FakeValidMessageQueue("errorQueue");
            var bus = new Bus(new FakeBusConfig { MaxRetries = 2, EnvironmentalErrorsOnly = true }, logger, errorQueue, QueueWithOneMessage("readQueue"), new[] { new FakeValidMessageQueue("writeQueue1") });
            bus.RegisterHandler(handler);

            bus.Receive<FakeDto>();

            Assert.That(logger[0], Does.Contain("Transaction started"));
            Assert.That(logger[1], Does.Contain("Started RECEIVE Operation"));
            Assert.That(logger[2], Does.Contain("Payload: FakeDto"));
            Assert.That(logger[3], Does.Contain("Invoking registered handler"));
            Assert.That(logger[4], Does.Contain("TRANSACTION STATUS: Active - REASON: The method or operation is not implemented."));
            Assert.That(logger[5], Does.Contain("Invocation failed"));
            Assert.That(logger[6], Does.Contain("Removing from read queue: readQueue"));
            Assert.That(logger[7], Does.Contain("Moving to error queue: errorQueue"));
            Assert.That(logger[8], Does.Contain("EXCEPTION - The method or operation is not implemented."));
            Assert.That(logger[10], Does.Contain("Completed RECEIVE Operation"));
            Assert.That(logger[11], Does.Contain("Transaction rolled back"));
        }

        [Test]
        public void Should_retry_when_configured_for_environmental_errors_only_and_error_is_environmental()
        {
            var logger = new FakeLogger();
            var handler = new FakeEnvironmentExceptionThrowingUserHandler();
            var errorQueue = new FakeValidMessageQueue("errorQueue");
            var bus = new Bus(new FakeBusConfig { MaxRetries = 2, EnvironmentalErrorsOnly = true}, logger, errorQueue, QueueWithOneMessage("readQueue"), new[] { new FakeValidMessageQueue("writeQueue1") });
            bus.RegisterHandler(handler);

            bus.Receive<FakeDto>();

            Assert.That(logger[0], Does.Contain("Transaction started"));
            Assert.That(logger[1], Does.Contain("Started RECEIVE Operation"));
            Assert.That(logger[2], Does.Contain("Payload: FakeDto"));
            Assert.That(logger[3], Does.Contain("Invoking registered handler"));
            Assert.That(logger[4], Does.Contain("TRANSACTION STATUS: Active - REASON: deadlocked"));
            Assert.That(logger[5], Does.Contain("Retry attempt 1"));
            Assert.That(logger[6], Does.Contain("Payload: FakeDto"));
            Assert.That(logger[7], Does.Contain("Invoking registered handler"));
            Assert.That(logger[8], Does.Contain("TRANSACTION STATUS: Active - REASON: deadlocked"));
            Assert.That(logger[9], Does.Contain("Retry attempt 2"));
            Assert.That(logger[10], Does.Contain("Payload: FakeDto"));
            Assert.That(logger[11], Does.Contain("Invoking registered handler"));
            Assert.That(logger[12], Does.Contain("TRANSACTION STATUS: Active - REASON: deadlocked"));
            Assert.That(logger[13], Does.Contain("Invocation failed"));
            Assert.That(logger[14], Does.Contain("Removing from read queue: readQueue"));
            Assert.That(logger[15], Does.Contain("Moving to error queue: errorQueue"));
            Assert.That(logger[16], Does.Contain("EXCEPTION - deadlocked"));
            Assert.That(logger[18], Does.Contain("Completed RECEIVE Operation"));
            Assert.That(logger[19], Does.Contain("Transaction rolled back"));
        }
    }

    [TestFixture]
    public class When_configuring_bus_builder
    {
        [Test]
        public void Should_error_if_neither_read_or_write_queues_defined()
        {
            var exception = Assert.Throws<ArgumentException>(() => new BusBuilder()
                .WithLogging(new FakeLogger())
                .EnlistInAmbientTransactions()
                .CreateLocalQueuesAutomatically()
                .JsonSerialization()
                .NumberOfRetries(3)                
                .CreateBus());

            Assert.That(exception.Message, Is.EqualTo("You need to supply at least one endpoint either to read from or to write to"));
        }

        [Test]
        public void Should_error_if_error_queue_not_defined()
        {
            var exception = Assert.Throws<ArgumentException>(() => new BusBuilder()
                .DefineReadQueue("x")
                .WithLogging(new FakeLogger())
                .EnlistInAmbientTransactions()
                .CreateLocalQueuesAutomatically()
                .JsonSerialization()
                .NumberOfRetries(3)
                .CreateBus());

            Assert.That(exception.Message, Is.EqualTo("You need to define an endpoint for error messages"));
        }
    }
}
