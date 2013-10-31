using System;
using System.Linq;
using System.Messaging;
using System.Security.Principal;
using MiniBus.Contracts;
using MiniBus.Logging;
using MiniBus.Exceptions;
using MiniBus.Infrastructure;
using System.Threading;
using MiniBus.MessageQueues;
using System.Collections.Generic;

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

        public BusBuilder DefineErrorQueue(string name)
        {
            _config.ErrorQueue = name;
            return this;
        }

        public BusBuilder NumberOfRetries(int maxRetries)
        {
            _config.MaxRetries = maxRetries;
            return this;
        }
        
        public BusBuilder WithLogging(ILogMessages logger)
        {
            _logger = logger;
            return this;
        }
        
        public BusBuilder InstallMsmqIfNeeded()
        {
            _config.InstallMsmq = true;
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

        public IBus CreateBus()
        {
            if (string.IsNullOrEmpty(_config.ReadQueue) && _config.WriteQueues.Count == 0)
                throw new ArgumentException("You need to supply at least one endpoint either to read from or to write to");

            if (string.IsNullOrEmpty(_config.ErrorQueue))
                throw new ArgumentException("You need to define an endpoint for error messages");

            InitializeMsmq(() =>
            {
                InititializeErrorQueue();
                InitializeReadQueue();
                InitializeWriteQueues();
            });

            return new Bus(_config, _logger, _errorQueue, _readQueue, _writeQueues);
        }

        void InitializeMsmq(Action onSuccess)
        {
            if (_config.InstallMsmq && !Msmq.IsInstalled)
            {
                try
                {
                    Msmq.Install();
                    onSuccess();
                }
                catch (Exception ex)
                {
                    _logger.Log(ex.Message);
                }
            }
            else
            {
                if (Msmq.IsInstalled)
                    onSuccess();
                else
                    _logger.Log("Msmq is not installed. Create the bus with InstallMsmqIfNeeded and run as administrator.");
            }
        }

        void InititializeErrorQueue()
        {
            if (!string.IsNullOrEmpty(_config.ErrorQueue))
            {
                string machineName = GetMachineName(_config.ErrorQueue);
                string queueName = GetQueueName(_config.ErrorQueue);
                string errorEndpointPath = FormatPathToQueue(".", _config.ErrorQueue);

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
                string readEndpointPath = FormatPathToQueue(".", _config.ReadQueue);

                CreateLocalEndpointOnDisk(".", readEndpointPath);
                ValidateQueue(machineName, queueName, readEndpointPath);
                CreateReadQueueFromPath(readEndpointPath);
            }
        }

        void InitializeWriteQueues()
        {
            if (_config.WriteQueues.Any())
            {
                foreach (var name in _config.WriteQueues)
                {
                    string machineName = GetMachineName(name);
                    string queueName = GetQueueName(name);
                    string writeEndpointPath = FormatPathToQueue(machineName, queueName);

                    CreateLocalEndpointOnDisk(machineName, writeEndpointPath);
                    ValidateQueue(machineName, queueName, writeEndpointPath);
                    CreateWriteQueueFromPath(writeEndpointPath);
                }
            }
        }

        static string GetMachineName(string queue)
        {
            string machineName = ".";

            if (queue.Contains("@"))
                machineName = queue.Substring(queue.IndexOf('@') + 1);

            return machineName;
        }

        static string GetQueueName(string queue)
        {
            string queueName = queue;

            if (queue.Contains("@"))
                queueName = queue.Substring(0, queue.IndexOf('@'));

            return queueName;
        }

        static string FormatPathToQueue(string machineName, string queueName)
        {
            if (machineName == ".") // local
                return string.Format(@".\private$\{0}", queueName);

            // remote
            return string.Format(@"FormatName:DIRECT=OS:{0}\private$\{1}", machineName, queueName);
        }

        void CreateLocalEndpointOnDisk(string machineName, string path)
        {
            // we'll create local queues if required and they don't already exist
            if (machineName == "." && _config.AutoCreateLocalQueues)
            {                
                if (MessageQueue.Exists(path)) return;

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
                throw new QueueNotFoundException(string.Format("{0} doesn't exist {1}. Did you type it correctly?", queueName, machineName == "." ? "locally" : "on " + machineName));
        }

        void CreateReadQueueFromPath(string path)
        {
            _readQueue = new MiniBusMessageQueue(new MessageQueue(path), _logger);
        }

        void CreateWriteQueueFromPath(string path)
        {
            _writeQueues.Add(new MiniBusMessageQueue(new MessageQueue(path), _logger));
        }

        void CreateQueueToPutErrorsOn(string machineName, string queueName, string path)
        {
            if (QueueExists(machineName, queueName, path))
                _errorQueue = new MiniBusMessageQueue(new MessageQueue(path), _logger);
            else
                throw new QueueNotFoundException(queueName + " doesn't exist. Did you type it correctly?");
        }

        static bool QueueExists(string machineName, string queueName, string path)
        {
            if (machineName == ".")
                return MessageQueue.Exists(path);

            // MessageQueue.Exists doesn't work for remote machines
            var results = MessageQueue.GetPrivateQueuesByMachine(machineName);
            return results.Any(q => q.QueueName == string.Format(@"private$\{0}", queueName));
        }

        List<IMessageQueue> _writeQueues = new List<IMessageQueue>();
        IMessageQueue _readQueue;
        IMessageQueue _errorQueue;
        IBusConfig _config;
        ILogMessages _logger = new NullLogger();
    }
}