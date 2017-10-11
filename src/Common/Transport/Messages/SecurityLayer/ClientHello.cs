using System;
using MessagePack;

namespace Ricotta.Transport.Messages.SecurityLayer
{
    [MessagePackObject]
    public class ClientHello
    {
        [Key(0)]
        public byte[] Random;
        [Key(1)]
        public string RSAPublicPem;
    }
}