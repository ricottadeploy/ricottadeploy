using System;
using System.Collections.Generic;
using MessagePack;

namespace Ricotta.Transport.Messages.Application
{
    [MessagePackObject]
    public class MasterAgentDeny
    {
        [Key(0)]
        public List<string> Agents;
    }
}