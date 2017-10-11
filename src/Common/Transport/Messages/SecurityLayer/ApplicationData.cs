using System;
using MessagePack;

namespace Ricotta.Transport.Messages.SecurityLayer
{
    [MessagePackObject]
    public class ApplicationData
    {
        [Key(0)]
        public string SessionId;
        [Key(1)]
        public byte[] AesIv;
        [Key(2)]
        public byte[] Data;
    }
}