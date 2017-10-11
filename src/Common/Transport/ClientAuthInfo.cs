using System;
using MessagePack;

namespace Ricotta.Transport
{
    [MessagePackObject]
    public class ClientAuthInfo
    {
        [Key(0)]
        public string ClientId;
        [Key(1)]
        public string RsaFingerprint;
        [Key(2)]
        public ClientStatus AuthenticationStatus;
    }
}