using System;
using MessagePack;

namespace Ricotta.Transport.Messages.Application
{
    [MessagePackObject]
    public class AgentJobResult
    {
        [Key(0)]
        public string AgentId;
        [Key(1)]
        public string JobId;
        [Key(2)]
        public int ErrorCode;   // 0 = Success
        [Key(3)]
        public string ErrorMessage;
        [Key(4)]
        public string ResultData;
    }
}