using System;
using MessagePack;

namespace Ricotta.Transport.Messages.Application
{
    [MessagePackObject]
    public class MasterFileInfo
    {
        [Key(0)]
        public string FileUri;
    }
}