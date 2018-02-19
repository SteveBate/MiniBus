using MiniBus.Contracts;
using log4net;
using System;
using System.Transactions;

namespace MiniBus.Sample
{
    class Program
    {
        static IBus _bus;

        static void Main()
        {
            bool exit = false;

            do
            {
                Console.Clear();
                Console.WriteLine("Choose an option to execute:");
                Console.WriteLine("1  - Send message to queue");
                Console.WriteLine("2  - Send message to queues in distributed manner");
                Console.WriteLine("3  - Read message from queue");
                Console.WriteLine("4  - Read message from queue async");
                Console.WriteLine("5  - Fail to error queue");
                Console.WriteLine("6  - Fail to error queue async");
                Console.WriteLine("7  - Fail fast");
                Console.WriteLine("8  - Fail fast async");
                Console.WriteLine("9  - Return error messages to read queue");
                Console.WriteLine("A  - Fail and discard");
                Console.WriteLine("B  - Test multiple handlers");
                Console.WriteLine("X  - exit");

                var selection = Console.ReadKey();

                switch (selection.Key)
                {
                    case ConsoleKey.D1:
                        SendDemo();
                        break;

                    case ConsoleKey.D2:
                        SendAutoDistributeDemo();
                        break;

                    case ConsoleKey.D3:
                        ReceiveDemo();
                        break;

                    case ConsoleKey.D4:
                        ReceiveDemoAsync();
                        break;

                    case ConsoleKey.D5:
                        FailToErrorQueueDemo();
                        break;

                    case ConsoleKey.D6:
                        FailToErrorQueueAsync();
                        break;

                    case ConsoleKey.D7:
                        FailFastDemo();
                        break;

                    case ConsoleKey.D8:
                        FailFastDemoAsync();
                        break;

                    case ConsoleKey.D9:
                        ReturnErrorMessagesDemo();
                        break;

                    case ConsoleKey.A:
                        FailAndDiscardDemo();
                        break;

                    case ConsoleKey.B:
                        ReceiveMultiplesDemo();
                        break;

                    case ConsoleKey.X:
                        exit = true;
                        break;

                    default:
                        Console.WriteLine("invalid option");
                        break;
                }
            } while (!exit);
        }

        static void SendDemo()
        {
            // demonstrate sending message to defined write queue

            _bus = new BusBuilder()
                .WithLogging(new FileLogger())
                .DefineErrorQueue("MiniBus.errors")
                .DefineWriteQueue("MiniBus.messages1")
                .CreateLocalQueuesAutomatically()
                .EnlistInAmbientTransactions()
                .JsonSerialization()
                .TimeToBeReceived(TimeSpan.FromMinutes(5))
                .CreateBus();

            
            // transaction scope isn't required unless modifying another transactional resource at the same time
            using (var scope = new TransactionScope())
            {
                for (int i = 1; i <= 10; i++)
                {
                    var p = new Person { Name = "Bob", Age = i };

                    // insert/update a database to see atomic commit

                    _bus.Send(p);
                }
                scope.Complete();
            }

            Console.WriteLine("\nMessages sent - press Enter to choose new option");
            Console.ReadLine();
            _bus.Dispose();
        }

        static void SendAutoDistributeDemo()
        {
            // demonstrate distributing messages evenly across queues
            // each call to Send chooses the next queue to send the message to

            _bus = new BusBuilder()
                .WithLogging(new FileLogger())
                .DefineErrorQueue("MiniBus.errors")
                .DefineWriteQueues("MiniBus.messages1", "MiniBus.messages2", "MiniBus.messages3")
                .CreateLocalQueuesAutomatically()
                .AutoDistributeOnSend()
                .JsonSerialization()
                .TimeToBeReceived(TimeSpan.FromMinutes(5))
                .CreateBus();

            for (int i = 0; i < 4; i++)
            {
                var p = new Person { Name = "Bob", Age = 20};
                _bus.Send(p);
            }

            Console.WriteLine("\nMessages sent - press Enter to choose new option");
            Console.ReadLine();
            _bus.Dispose();
        }

        static void ReceiveDemo()
        {
            // demonstrate receiving - Receive processes whatever messages happen to be on the queue at the time of the call

            Console.WriteLine("\nPress enter to read message/s");
            Console.ReadLine();

            _bus = new BusBuilder()
                .WithLogging(new FileLogger())
                .DefineErrorQueue("MiniBus.errors")
                .DefineReadQueue("MiniBus.messages1")
                .CreateLocalQueuesAutomatically()
                .EnlistInAmbientTransactions()
                .JsonSerialization()
                .NumberOfRetries(3)
                .CreateBus();

            // pass true to have a message fail and moved to the error queue
            _bus.RegisterHandler(new PersonHandler(false));

            _bus.Receive<Person>();

            Console.WriteLine("\nMessages read - Press Enter to choose new option");
            Console.ReadLine(); 
            _bus.Dispose();
        }

        static void ReceiveDemoAsync()
        {
            // demonstrate receiving asynchronously - ReceiveAsync processes messages as and when they arrive. 
            // Open another instance to send messages while the other receives.

            _bus = new BusBuilder()
                .WithLogging(new ConsoleLogger())
                .DefineErrorQueue("MiniBus.errors")
                .DefineReadQueue("MiniBus.messages1")
                .CreateLocalQueuesAutomatically()
                .EnlistInAmbientTransactions()
                .JsonSerialization()
                .NumberOfRetries(3)
                .CreateBus();

            Console.WriteLine("\nListening...");

            // pass true to have a message fail and moved to the error queue
            _bus.RegisterHandler(new PersonHandler(false));
            _bus.ReceiveAsync<Person>();

            Console.WriteLine("\nPress Enter to choose new option");
            Console.ReadLine();
            _bus.StopReceiving();
            _bus.Dispose();
        }

        static void FailToErrorQueueDemo()
        {
            // demonstrate error whilst receiving - PersonHandler will throw exception moving the message to the error queue

            Console.WriteLine("\nPress a key to read message/s");
            Console.ReadLine();

            _bus = new BusBuilder()
                .WithLogging(new FileLogger())
                .DefineErrorQueue("MiniBus.errors")
                .DefineReadQueue("MiniBus.messages1")
                .CreateLocalQueuesAutomatically()
                .EnlistInAmbientTransactions()
                .JsonSerialization()
                .NumberOfRetries(3)
                .CreateBus();

            // pass true to have a message fail and moved to the error queue
            _bus.RegisterHandler(new PersonHandler(true));

            _bus.Receive<Person>();

            Console.WriteLine("\nPress Enter to choose new option");
            Console.ReadLine();
            _bus.Dispose();
        }

        static void FailToErrorQueueAsync()
        {
            // demonstrate error whilst receiving asynchronously - PersonHandler will throw exception moving the message to the error queue
            // Open another instance to send messages while the other receives.

            _bus = new BusBuilder()
                .WithLogging(new ConsoleLogger())
                .DefineErrorQueue("MiniBus.errors")
                .DefineReadQueue("MiniBus.messages1")
                .CreateLocalQueuesAutomatically()
                .EnlistInAmbientTransactions()
                .JsonSerialization()
                .NumberOfRetries(3)
                .CreateBus();

            Console.WriteLine("\nListening...");

            // pass true to have a message fail and moved to the error queue
            _bus.RegisterHandler(new PersonHandler(true));
            _bus.ReceiveAsync<Person>();

            Console.WriteLine("\nPress Enter to choose new option");
            Console.ReadLine();
            _bus.StopReceiving();
            _bus.Dispose();
        }

        static void FailFastDemo()
        {
            // demonstrate fail fast option - PersonHandler throws exception but FailFast setting causes queue processing to stop leaving the message at the tip of the queue

            Console.WriteLine("\nPress a key to read message/s");
            Console.ReadLine();

            _bus = new BusBuilder()
                .WithLogging(new FileLogger())
                .DefineErrorQueue("MiniBus.errors")
                .DefineReadQueue("MiniBus.messages1")
                .CreateLocalQueuesAutomatically()
                .EnlistInAmbientTransactions()
                .JsonSerialization()
                .NumberOfRetries(3)
                .FailFast()
                .CreateBus();

            // pass true to have a message fail fast by stopping all message processing. Messages are left on the read queue
            _bus.RegisterHandler(new PersonHandler(true));

            _bus.Receive<Person>();

            Console.WriteLine("\nPress Enter to choose new option");
            Console.ReadLine();
            _bus.Dispose();
        }

        static void FailFastDemoAsync()
        {
            // demonstrate fail fast option whilst receiving asynchronously - PersonHandler throws exception but FailFast setting causes queue processing to stop leaving the message at the tip of the queue
            // Open another instance to send messages while the other receives.

            _bus = new BusBuilder()
                .WithLogging(new ConsoleLogger())
                .DefineErrorQueue("MiniBus.errors")
                .DefineReadQueue("MiniBus.messages1")
                .CreateLocalQueuesAutomatically()
                .EnlistInAmbientTransactions()
                .JsonSerialization()
                .NumberOfRetries(3)
                .FailFast()
                .CreateBus();

            // pass true to have a message fail fast by stopping all message processing. Messages are left on the read queue
            _bus.RegisterHandler(new PersonHandler(true));
            _bus.ReceiveAsync<Person>();

            Console.WriteLine("\nPress Enter to choose new option");
            Console.ReadLine();
            _bus.Dispose();
        }

        static void ReturnErrorMessagesDemo()
        {
            // demonstrate moving messages from error queue to read queue

            _bus = new BusBuilder()
                .WithLogging(new FileLogger())
                .DefineErrorQueue("MiniBus.errors")
                .DefineReadQueue("MiniBus.messages1")
                .CreateLocalQueuesAutomatically()
                .CreateBus();

            Console.WriteLine("\nEnter the id of the message to be returned or type 'all' for all messages");
            string messageid = Console.ReadLine();

            try
            {
                if (messageid == "all")
                {
                    _bus.ReturnAllErrorMessages();
                }
                else
                {
                    _bus.ReturnErrorMessage(messageid);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Couldn't return error messages to the read queue: {0}", ex.Message);
            }

            Console.WriteLine("\nPress Enter to choose new option");
            Console.ReadLine();
            _bus.Dispose();
        }

        static void FailAndDiscardDemo()
        {
            // demonstrate DiscardFailedMessages option - PersonHandler throws exception but message is thrown away

            Console.WriteLine("\nPress a key to read message/s");
            Console.ReadLine();

            _bus = new BusBuilder()
                .WithLogging(new FileLogger())
                .DefineErrorQueue("MiniBus.errors")
                .DefineReadQueue("MiniBus.messages1")
                .CreateLocalQueuesAutomatically()
                .EnlistInAmbientTransactions()
                .JsonSerialization()
                .NumberOfRetries(3)
                .DiscardFailedMessages()
                .CreateBus();

            _bus.RegisterHandler(new PersonHandler(true));

            _bus.Receive<Person>();

            Console.WriteLine("\nPress Enter to choose new option");
            Console.ReadLine();
            _bus.Dispose();
        }

        static void ReceiveMultiplesDemo()
        {
            // demonstrate receiving in multiple handlers

            Console.WriteLine("\nPress a key to read message/s");
            Console.ReadLine();

            _bus = new BusBuilder()
                .WithLogging(new FileLogger())
                .DefineErrorQueue("MiniBus.errors")
                .DefineReadQueue("MiniBus.basicmessages")
                .DefineWriteQueue("MiniBus.basicmessages")
                .CreateLocalQueuesAutomatically()
                .EnlistInAmbientTransactions()
                .JsonSerialization()
                .NumberOfRetries(3)
                .DiscardFailedMessages()
                .CreateBus();

            // register different handlers
            _bus.RegisterHandler(new NumberHandler());
            _bus.RegisterHandler(new SquaredNumberHandler());
            _bus.RegisterHandler(new CubedNumberHandler());

            _bus.ReceiveAsync<int>();

            _bus.Send(101);
            _bus.Send(42);
            _bus.Send(18);
            _bus.Send(5);

            Console.WriteLine("\nPress Enter to choose new option");
            Console.ReadLine();
            _bus.Dispose();
        }
    }

    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public override string ToString()
        {
            return $"Name: {Name}, Age: {Age}";
        }
    }

    class ConsoleLogger : ILogMessages
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }

    class FileLogger : ILogMessages
    {
        public void Log(string message)
        {
            if(Logger.IsDebugEnabled)
                Logger.Debug(message);
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));
    }

    class PersonHandler : IHandleMessage<Person>
    {
        public PersonHandler(bool throwException)
        {
            _throwException = throwException;
        }

        public void Handle(Person p)
        {
            if (p.Age == 3 && _throwException)
            {
                throw new Exception("oh oh!");
            }

            Console.WriteLine("Recevied person: {0} aged: {1}", p.Name, p.Age);
        }

        readonly bool _throwException;
    }
    
    class NumberHandler : IHandleMessage<int>
    {
        public void Handle(int x)
        {
            Console.WriteLine("Received number: {0}", x);
        }
    }

    class SquaredNumberHandler : IHandleMessage<int>
    {
        public void Handle(int x)
        {
            Console.WriteLine("Received number squared: {0} x {0} = {1}", x, x * x);
        }
    }

    class CubedNumberHandler : IHandleMessage<int>
    {
        public void Handle(int x)
        {
            Console.WriteLine("Received number cubed: {0} x {0} x {0} = {1}", x,  x * x * x);
        }
    }
}
