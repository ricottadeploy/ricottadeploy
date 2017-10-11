using System;
using MessagePack;

namespace Ricotta.Transport.Messages.Application
{
    [MessagePackObject]
    public class CommandAgentList
    {
        [Key(0)]
        public string Filter;
    }
}