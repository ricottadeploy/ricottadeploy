using System;
using MessagePack;

namespace Ricotta.Transport.Messages.Application
{
    [MessagePackObject]
    public class CommandAgentDeny
    {
        [Key(0)]
        public string Selector;
    }
}