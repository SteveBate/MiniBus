using System;
using System.Linq;
using System.Messaging;
using System.Security.Principal;
using MiniBus.Contracts;
using MiniBus.Logging;
using MiniBus.Exceptions;
using System.Threading;
using MiniBus.MessageQueues;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text.RegularExpressions;

namespace MiniBus
{
    public class BusBuilder
    {
        public BusBuilder()
        {
            _config = new BusConfig();
        }

        /// <summary>
        /// DefineReadQueue - Specify the name of a queue where messages will be read from and passed to your handler
        /// </summary>
        /// <param name="name">The name of a queue</param>
        /// <returns></returns>
        public BusBuilder DefineReadQueue(string name)
        {
            _config.ReadQueue = name;
            return this;
        }

        /// <summary>
        /// DefineWriteQueue - Specify a queue where messages will be sent to
        /// </summary>
        /// <param name="name">The name of a queue. When the queue is on a remote machine use the format name@machine</param>
        /// <returns></returns>
        public BusBuilder DefineWriteQueue(string name)
        {
            _config.WriteQueues.Add(name);
            return this;
        }

        /// <summary>
        /// DefineWriteQueues - Specify in one method all the queues to be written to
        /// </summary>
        /// <param name="names">The names of all the write queues. When the queue is on a remote machine use the format name@machine</param>
        /// <returns></returns>
        public BusBuilder DefineWriteQueues(params string[] names)
        {
            foreach (var name in names)
            {
                _config.WriteQueues.Add(name);
            }

            return this;
        }

        /// <summary>
        /// DefineErrorQueue - Specify the queue where messages should be eventually placed after all retry attempts have failed
        /// </summary>
        /// <param name="name">The name of the error queue</param>
        public BusBuilder DefineErrorQueue(string name)
        {
            _config.ErrorQueue = name;
            return this;
        }

        /// <summary>
        /// NumberOfRetries - specify how many times MiniBus should attempt to process the message
        /// </summary>
        /// <param name="maxRetries">Maximumn number of times to to retry</param>
        /// <param name="slidingRetryInterval">number of milliseconds to wait between retries multiplied by maxRetries</param>
        /// <param name="environmentalErrorsOnly">Only retry when error is realistically recoverable - to maintain backwards compatability the default value is false</param>
        public BusBuilder NumberOfRetries(int maxRetries, int slidingRetryInterval = 0, bool environmentalErrorsOnly = false)
        {
            _config.MaxRetries = maxRetries;
            _config.SlidingRetryInterval = slidingRetryInterval;
            _config.EnvironmentalErrorsOnly = environmentalErrorsOnly;
            return this;
        }

        /// <summary>
        /// WithLogging - The object that will be responsible for outputting log information
        /// </summary>
        /// <param name="logger">The instance of an object implmenting the ILogMessage interface</param>
        public BusBuilder WithLogging(ILogMessages logger)
        {
            _logger = logger;
            return this;
        }

        /// <summary>
        /// CreateLocalQueuesAutomatically - Specify this option to have the queues created for you of they don't already exists
        /// </summary>
        public BusBuilder CreateLocalQueuesAutomatically()
        {
            _config.AutoCreateLocalQueues = true;
            return this;
        }

        /// <summary>
        /// EnlistInAmbientTransactions - Specify this option to join an existing transaction and
        /// ensure all operations are either committed or rolled back together
        /// </summary>
        public BusBuilder EnlistInAmbientTransactions()
        {
            _config.EnlistInAmbientTransactions = true;
            return this;
        }

        /// <summary>
        /// JsonSerialization - Specify this option to have messages sent in the JSON format. The default is XML.
        /// XML requires the type to be shared between the sender and the receiver. JSON is a looser format with
        /// no such requirement
        /// </summary>
        public BusBuilder JsonSerialization()
        {
            _config.JsonSerialization = true;
            return this;
        }

        /// <summary>
        /// TimeToBeReceived - Specify this option to determine how long the message will live on the queue for before automatically expiring
        /// </summary>
        /// <param name="span">A TimeSpan value</param>
        public BusBuilder TimeToBeReceived(TimeSpan span)
        {
            _config.TimeToBeReceived = span;
            return this;
        }

        /// <summary>
        /// AutoDistributeOnSend - Specify this option to have messages sent to a different queue on each call to send to provide
        /// rudimentary roud-robin load balancing. When using the Send overload to pass in a destination queue this operation is
        /// not applicable
        /// </summary>
        public BusBuilder AutoDistributeOnSend()
        {
            _config.AutoDistributeOnSend = true;
            return this;
        }

        /// <summary>
        /// FailFast - Stop processing the queue immediately. Leaves the message on the queue rather than move to an error queue.
        /// This can be important when order of messages is important.
        /// </summary>
        public BusBuilder FailFast()
        {
            _config.FailFast = true;
            return this;
        }

        /// <summary>
        /// DiscardFailedMessages - Specify this option to drop error messages rather than move them to the error queue
        /// </summary>
        public BusBuilder DiscardFailedMessages()
        {
            _config.DiscardFailures = true;
            return this;
        }

        /// <summary>
        /// UseJournalQueue - Specify this to indicate that a copy of the message should be kept in the machine journal of the sending computer
        /// </summary>
        public BusBuilder UseJournalQueue()
        {
            _config.UseJournalQueue = false;
            return this;
        }

        /// <summary>
        /// OnErrorAsync - Provides a means to have one or more user actions invoked when an error occurs
        /// </summary>
        /// <param name="actions">one or more void methods that take a string parameter</param>
        public BusBuilder OnErrorAsync(params Action<string>[] actions)
        {
            _config.ErrorActions.AddRange(actions);
            return this;
        }

        /// <summary>
        /// AutoPurgeSystemJournal - Specify this to have MiniBus keep on top of too many messages causing MSMQ to grind to a halt
        /// </summary>
        public BusBuilder AutoPurgeSystemJournal()
        {
            _config.AutoPurgeSystemJournal = true;
            return this;
        }

        /// <summary>
        /// CreateBus - Create and return a configured bus ready to send or receive messages
        /// </summary>
        public IBus CreateBus()
        {
            GuardAgainstInvalidQueueStates();

            IsMsmqInstalled(() =>
            {
                InititializeErrorQueue();
                InitializeReadQueue();
                InitializeWriteQueues();
            });

            return new Bus(_config, _logger, _errorQueue, _readQueue, _writeQueues);
        }

        void IsMsmqInstalled(Action onSuccess)
        {
            if (ServiceController.GetServices().Any(s => s.ServiceName == "MSMQ"))
                onSuccess();
            else
                throw new BusException("Msmq is not installed!");
        }

        void InititializeErrorQueue()
        {
            if (!string.IsNullOrEmpty(_config.ErrorQueue))
            {
                string machineName = GetMachineName(_config.ErrorQueue);
                string queueName = GetQueueName(_config.ErrorQueue);
                string errorEndpointPath = FormatPathToQueue(LocalMachine, _config.ErrorQueue);

                CreateLocalEndpointOnDisk(machineName, errorEndpointPath);
                CreateQueueToPutErrorsOn(machineName, queueName, errorEndpointPath);
            }
        }

        void InitializeReadQueue()
        {
            if (!string.IsNullOrEmpty(_config.ReadQueue))
            {
                string machineName = GetMachineName(_config.ReadQueue);
                string queueName = GetQueueName(_config.ReadQueue);
                string readEndpointPath = FormatPathToQueue(machineName, queueName);

                CreateLocalEndpointOnDisk(machineName, readEndpointPath);
                ValidateQueue(machineName, queueName, readEndpointPath);
                CreateReadQueueFromPath(readEndpointPath);
            }
        }

        void InitializeWriteQueues()
        {
            _config.WriteQueues.ForEach(item =>
            {
                string machineName = GetMachineName(item);
                string queueName = GetQueueName(item);
                string writeEndpointPath = FormatPathToQueue(machineName, queueName);

                CreateLocalEndpointOnDisk(machineName, writeEndpointPath);
                ValidateQueue(machineName, queueName, writeEndpointPath);
                CreateWriteQueueFromPath(writeEndpointPath);
            });
        }

        static string GetMachineName(string queue)
        {
            return queue.Contains("@") ? queue.Substring(queue.IndexOf('@') + 1) : LocalMachine;
        }

        static string GetQueueName(string queue)
        {
            return queue.Contains("@") ? queue.Substring(0, queue.IndexOf('@')) : queue;
        }

        static string FormatPathToQueue(string machine, string queue)
        {
            string LocalQueue(string queueName)
            {
                return $@".\private$\{queueName}";
            }

            string RemoteQueue(string machineName, string queueName)
            {
                bool isIpAddress = Regex.Match(machineName, Ipaddress).Success;
                string transport = isIpAddress ? "TCP" : "OS";
                return $@"FormatName:DIRECT={transport}:{machineName}\private$\{queueName}";
            }

            return machine == LocalMachine ? LocalQueue(queue) : RemoteQueue(machine, queue);
        }

        void GuardAgainstInvalidQueueStates()
        {
            if (string.IsNullOrEmpty(_config.ReadQueue) && _config.WriteQueues.Count == 0)
            {
                throw new ArgumentException("You need to supply at least one endpoint either to read from or to write to");
            }

            if (string.IsNullOrEmpty(_config.ErrorQueue))
            {
                throw new ArgumentException("You need to define an endpoint for error messages");
            }
        }

        void CreateLocalEndpointOnDisk(string machineName, string path)
        {
            // we'll create local queues if required and they don't already exist
            if (machineName == LocalMachine && _config.AutoCreateLocalQueues)
            {
                if (MessageQueue.Exists(path)) { return; }

                // create and set permissions
                using (var queue = MessageQueue.Create(path, true))
                {
                    string adminName = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null).Translate(typeof(NTAccount)).ToString();
                    queue.SetPermissions(Thread.CurrentPrincipal.Identity.Name, MessageQueueAccessRights.GenericWrite);
                    queue.SetPermissions(adminName, MessageQueueAccessRights.FullControl);
                }
            }
        }

        void ValidateQueue(string machineName, string queueName, string path)
        {
            if (!QueueExists(machineName, queueName, path))
            {
                throw new QueueNotFoundException(
                    $"{queueName} doesn't exist {(machineName == LocalMachine ? "locally" : "on " + machineName)}. Did you type it correctly?");
            }
        }

        void CreateReadQueueFromPath(string path)
        {
            _readQueue = new MiniBusMessageQueue(new MessageQueue(path), _logger, _config.AutoPurgeSystemJournal);
        }

        void CreateWriteQueueFromPath(string path)
        {
            _writeQueues.Add(new MiniBusMessageQueue(new MessageQueue(path), _logger, _config.AutoPurgeSystemJournal));
        }

        void CreateQueueToPutErrorsOn(string machineName, string queueName, string path)
        {
            if (QueueExists(machineName, queueName, path))
            {
                _errorQueue = new MiniBusMessageQueue(new MessageQueue(path), _logger, _config.AutoPurgeSystemJournal);
            }
            else
            {
                throw new QueueNotFoundException(queueName + " doesn't exist. Did you type it correctly?");
            }
        }

        static bool QueueExists(string machineName, string queueName, string path)
        {
            bool isIpAddress = Regex.Match(machineName, Ipaddress).Success;

            if (isIpAddress)
            {
                return true; // assume valid as no means of retrieving queues when using ipaddress
            }

            if (machineName == LocalMachine)
            {
                return MessageQueue.Exists(path);
            }

            // MessageQueue.Exists doesn't work for remote machines
            var results = MessageQueue.GetPrivateQueuesByMachine(machineName);
            return results.Any(q => q.QueueName == $@"private$\{queueName}");
        }

        readonly List<IMessageQueue> _writeQueues = new List<IMessageQueue>();
        readonly IBusConfig _config;

        IMessageQueue _readQueue;
        IMessageQueue _errorQueue;
        ILogMessages _logger = new NullLogger();

        const string Ipaddress = @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b";
        const string LocalMachine = ".";
    }

}