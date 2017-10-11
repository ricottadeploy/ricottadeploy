using System;
using MessagePack;

namespace Ricotta.Transport.Messages.Application
{
    [MessagePackObject]
    public class ApplicationMessage
    {
        [Key(0)]
        public ApplicationMessageType Type;
        [Key(1)]
        public byte[] Data;
    }
}