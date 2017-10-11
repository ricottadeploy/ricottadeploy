using System;
using MessagePack;

namespace Ricotta.Transport.Messages.SecurityLayer
{
    [MessagePackObject]
    public class ServerHello
    {
        [Key(0)]
        public string SessionId;
        [Key(1)]
        public byte[] Random;
        [Key(2)]
        public string RSAPublicPem;
    }
}