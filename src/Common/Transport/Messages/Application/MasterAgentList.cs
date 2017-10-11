using System;
using System.Collections.Generic;
using MessagePack;

namespace Ricotta.Transport.Messages.Application
{
    [MessagePackObject]
    public class MasterAgentList
    {
        [Key(0)]
        public List<ClientAuthInfo> Agents;
    }
}