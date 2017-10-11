using System;
using MessagePack;

namespace Ricotta.Transport.Messages.Application
{
    [MessagePackObject]
    public class AgentModuleInfo
    {
        [Key(0)]
        public string ModuleName;
    }
}