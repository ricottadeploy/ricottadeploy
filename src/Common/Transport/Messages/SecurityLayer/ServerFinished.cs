using System;
using MessagePack;

namespace Ricotta.Transport.Messages.SecurityLayer
{
    [MessagePackObject]
    public class ServerFinished
    {
        [Key(0)]
        public string SessionId;
    }
}