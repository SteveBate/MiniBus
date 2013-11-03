using MiniBus.Contracts;
using log4net;
using System;
using System.Transactions;

namespace MiniBus.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Choose an option to execute:");
            Console.WriteLine("1 - Send message to queue");
            Console.WriteLine("2 - Send message to queues in distributed manner");
            Console.WriteLine("3 - Read message from queue");
            Console.WriteLine("4 - Read message from queue async");
            Console.WriteLine("5 - Return error message to read queue");
            Console.WriteLine("X - exit");

            var selection = Console.ReadKey();

            switch(selection.Key)
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
                    ReturnErrorMessagesDemo();
                    break;

                case ConsoleKey.X:                    
                    break;

                default:
                    Console.WriteLine("invalid option");
                    break;
            }
                        
            Console.ReadLine();
        }

        static void SendDemo()
        {
            // demonstrate sending message to defined write queue

            IBus bus = new BusBuilder()
                .WithLogging(new FileLogger())
                .InstallMsmqIfNeeded()
                .DefineErrorQueue("MiniBus.errors")
                .DefineWriteQueue("MiniBus.messages1")
                .CreateLocalQueuesAutomatically()
                .EnlistInAmbientTransactions()
                .JsonSerialization()
                .CreateBus();

            
            // transaction scope isn't required unless modifying another transactional resource at the same time
            using (var scope = new TransactionScope())
            {
                for (int i = 1; i <= 10; i++)
                {
                    var p = new Person { Name = "Bob", Age = i };

                    // insert/update a database to see atomic commit

                    bus.Send(p);
                }
                scope.Complete();
            }
            
            Console.WriteLine("\nPress a key to exit");
            Console.ReadLine();
            bus.Dispose();
        }

        static void SendAutoDistributeDemo()
        {
            // demonstrate distributing messages evenly across queues
            // each call to Send chooses the next queue to send the message to

            IBus bus = new BusBuilder()
                .WithLogging(new FileLogger())
                .InstallMsmqIfNeeded()
                .DefineErrorQueue("MiniBus.errors")
                .DefineWriteQueue("MiniBus.messages1")
                .DefineWriteQueue("MiniBus.messages2")
                .DefineWriteQueue("MiniBus.messages3")
                .CreateLocalQueuesAutomatically()
                .AutoDistributeOnSend()
                .JsonSerialization()
                .CreateBus();

            var p = new Person { Name = "Bob", Age = 20 };
            bus.Send(p); // MiniBus.messages1
            bus.Send(p); // MiniBus.messages2
            bus.Send(p); // MiniBus.messages3
            bus.Send(p); // MiniBus.messages1
            
            Console.WriteLine("\nPress a key to exit");
            Console.ReadLine();
            bus.Dispose();
        }

        static void ReceiveDemo()
        {
            // demonstrate receiving - Receive processes whatever messages happen to be on the queue at the time of the call

            Console.WriteLine("\nPress a key to read message/s");
            Console.ReadLine();

            IBus bus = new BusBuilder()
                .WithLogging(new FileLogger())
                .InstallMsmqIfNeeded()
                .DefineErrorQueue("MiniBus.errors")
                .DefineReadQueue("MiniBus.messages1")
                .CreateLocalQueuesAutomatically()
                .EnlistInAmbientTransactions()
                .JsonSerialization()
                .NumberOfRetries(3)
                .CreateBus();

            // pass true to have a message fail and moved to the error queue
            bus.RegisterHandler(new PersonHandler(false));
            
            bus.Receive<Person>();

            Console.WriteLine("\nPress a key to exit");
            Console.ReadLine(); 
            bus.Dispose();
        }

        static void ReceiveDemoAsync()
        {
            // demonstrate receiving asynchrnously - ReceiveAsync processes messages as and when they arrive. 
            // Open another instance to send messages while the other receives.

            IBus bus = new BusBuilder()
                .WithLogging(new ConsoleLogger())
                .InstallMsmqIfNeeded()
                .DefineErrorQueue("MiniBus.errors")
                .DefineReadQueue("MiniBus.messages1")
                .CreateLocalQueuesAutomatically()
                .EnlistInAmbientTransactions()
                .JsonSerialization()
                .NumberOfRetries(3)
                .CreateBus();

            bus.RegisterHandler(new PersonHandler(false));
            bus.ReceiveAsync<Person>();

            Console.WriteLine("\nPress a key to exit");
            Console.ReadLine();
            bus.Dispose();
        }

        static void ReturnErrorMessagesDemo()
        {
            // demonstrate moving messages from error queue to read queue

            IBus bus = new BusBuilder()
                .WithLogging(new FileLogger())
                .DefineErrorQueue("MiniBus.errors")
                .DefineReadQueue("MiniBus.messages1")
                .CreateLocalQueuesAutomatically()
                .CreateBus();

            bus.ReturnErrorMessages();

            Console.WriteLine("\nPress a key to exit");
            Console.ReadLine();
            bus.Dispose();
        }
    }

    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }

        public override string ToString()
        {
            return string.Format("Name: {0}, Age: {1}", Name, Age);
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
            if(log.IsDebugEnabled)
                log.Debug(message);
        }

        static readonly ILog log = LogManager.GetLogger(typeof(Program));
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

        bool _throwException;
    }    
}
