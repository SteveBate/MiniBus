using System;
using System.Collections.Generic;
using MiniBus.Contracts;

namespace MiniBus.Tests.Fakes
{
    public sealed class FakeBusConfig : IBusConfig
    {
        public int MaxRetries { get; set; }
        public int SlidingRetryInterval { get; set; }
        public string ReadQueue { get; set; }
        public string ErrorQueue { get; set; }
        public List<string> WriteQueues { get; set; }
        public List<Action<string>> ErrorActions { get; set; }
        public bool AutoCreateLocalQueues { get; set; }
        public bool InstallMsmq { get; set; }
        public bool EnlistInAmbientTransactions { get; set; }
        public bool JsonSerialization { get; set; }
        public bool AutoDistributeOnSend { get; set; }
        public bool FailFast { get; set; }
        public bool DiscardFailures { get; set; }
        public bool AutoPurgeSystemJournal { get; set; }
        public TimeSpan TimeToBeReceived { get; set; }
        public bool UseJournalQueue { get; set; }
        public bool UseDeadLetterQueue { get; set; }
        public bool EnvironmentalErrorsOnly { get; set; }
        public bool RequireNewTransaction { get; set; }
    }
}
