using MiniBus.Contracts;
using System;
using System.Collections.Generic;

namespace MiniBus
{
    internal class BusConfig : IBusConfig
    {
        public BusConfig()
        {
            WriteQueues = new List<string>();
            ErrorActions = new List<Action<string>>();
        }

        public int MaxRetries { get; set; }
        public string ReadQueue { get; set; }
        public string ErrorQueue { get; set; }
        public List<string> WriteQueues { get; private set; }
        public bool AutoCreateLocalQueues { get; set; }
        public bool InstallMsmq { get; set; }
        public bool EnlistInAmbientTransactions { get; set; }
        public bool JsonSerialization { get; set; }
        public bool AutoDistributeOnSend { get; set; }
        public bool FailFast { get; set; }
        public bool DiscardFailures { get; set; }
        public List<Action<string>> ErrorActions { get; set; }
        public bool AutoPurgeSystemJournal { get; set; }
    }
}
