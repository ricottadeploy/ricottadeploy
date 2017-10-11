using System;
using MessagePack;

namespace Ricotta.Transport.Messages.Application
{
    [MessagePackObject]
    public class MasterFileInfo
    {
        [Key(0)]
        public long Size;
        [Key(1)]
        public bool IsDirectory;
        [Key(2)]
        public string Sha256;
    }
}