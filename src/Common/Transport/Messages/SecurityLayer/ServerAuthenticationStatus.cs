using System;
using MessagePack;

namespace Ricotta.Transport.Messages.SecurityLayer
{
    [MessagePackObject]
    public class ServerAuthenticationStatus
    {
        [Key(0)]
        public ClientStatus Status;
    }
}