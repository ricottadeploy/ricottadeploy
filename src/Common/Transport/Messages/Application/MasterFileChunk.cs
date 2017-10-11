using System;
using MessagePack;

namespace Ricotta.Transport.Messages.Application
{
    [MessagePackObject]
    public class MasterFileChunk
    {
        [Key(0)]
        public string FileUri;
    }
}