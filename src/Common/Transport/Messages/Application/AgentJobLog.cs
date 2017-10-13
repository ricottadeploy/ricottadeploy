using System;
using MessagePack;

namespace Ricotta.Transport.Messages.Application
{
    [MessagePackObject]
    public class AgentJobLog
    {
        [Key(0)]
        public string AgentId;
        [Key(1)]
        public string JobId;
        [Key(2)]
        public string Message;
    }
}