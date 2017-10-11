using System;

namespace Ricotta.Transport
{
    public class Session
    {
        public string Id { get; set; }
        public byte[] ClientRandom { get; set; }
        public byte[] ServerRandom { get; set; }
        public byte[] MasterSecret { get; set; }
        public byte[] ClientWriteMACKey { get; set; }
        public byte[] ServerWriteMACKey { get; set; }
        public byte[] ClientWriteKey {get;set;}
        public byte[] ServerWriteKey {get;set;}
        public string RSAPublicPem { get; set; } // Other side
        public bool IsAuthenticated { get; set; }
    }
}