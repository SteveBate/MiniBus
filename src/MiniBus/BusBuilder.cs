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

        public BusBuilder DefineReadQueue(string name)
        {
            _config.ReadQueue = name;
            return this;
        }

        public BusBuilder DefineWriteQueue(string name)
        {
            _config.WriteQueues.Add(name);
            return this;
        }

        public BusBuilder DefineWriteQueues(params string[] names)
        {
            foreach (var name in names)
            {
                _config.WriteQueues.Add(name);
            }

            return this;
        }

        public BusBuilder DefineErrorQueue(string name)
        {
            _config.ErrorQueue = name;
            return this;
        }

        public BusBuilder NumberOfRetries(int maxRetries, int slidingRetryInterval = 0)
        {
            _config.MaxRetries = maxRetries;
            _config.SlidingRetryInterval = slidingRetryInterval;
            return this;
        }

        public BusBuilder WithLogging(ILogMessages logger)
        {
            _logger = logger;
            return this;
        }

        public BusBuilder CreateLocalQueuesAutomatically()
        {
            _config.AutoCreateLocalQueues = true;
            return this;
        }

        public BusBuilder EnlistInAmbientTransactions()
        {
            _config.EnlistInAmbientTransactions = true;
            return this;
        }

        public BusBuilder JsonSerialization()
        {
            _config.JsonSerialization = true;
            return this;
        }

        public BusBuilder TimeToBeReceived(TimeSpan span)
        {
            _config.TimeToBeReceived = span;
            return this;
        }

        public BusBuilder AutoDistributeOnSend()
        {
            _config.AutoDistributeOnSend = true;
            return this;
        }

        public BusBuilder FailFast()
        {
            _config.FailFast = true;
            return this;
        }

        public BusBuilder DiscardFailedMessages()
        {
            _config.DiscardFailures = true;
            return this;
        }

        public BusBuilder UseJournalQueue()
        {
            _config.UseJournalQueue = false;
            return this;
        }

        public BusBuilder OnErrorAsync(params Action<string>[] actions)
        {
            _config.ErrorActions.AddRange(actions);
            return this;
        }

        public BusBuilder AutoPurgeSystemJournal()
        {
            _config.AutoPurgeSystemJournal = true;
            return this;
        }

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
                string readEndpointPath = FormatPathToQueue(LocalMachine, _config.ReadQueue);

                CreateLocalEndpointOnDisk(LocalMachine, readEndpointPath);
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

        const string Ipaddress = "(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
        const string LocalMachine = ".";
    }

}