using System;
using MessagePack;

namespace Ricotta.Transport.Messages.SecurityLayer
{
    [MessagePackObject]
    public class SecurityLayerMessage
    {
        [Key(0)]
        public SecurityMessageType Type;
        [Key(1)]
        public byte[] Data;
    }
}