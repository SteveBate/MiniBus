using System;
using System.Collections.Generic;
using MSMQ.Messaging;
using MiniBus.Contracts;
using MiniBus.Core;

namespace MiniBus
{
    internal class MessageContext : BaseMessage
    {
        public Message Message { get; set; }
        public IMessageQueue ReadQueue { get; set; }
        public IMessageQueue WriteQueue { get; set; }
        public IMessageQueue ErrorQueue { get; set; }
        public IBusConfig Config { get; set; }
        public IEnumerable<Delegate> Handlers { get; set; }
        public string OpType { get; set; }
        public bool FailFast { get; set; }
        public bool Handled { get; set; }
    }
}
