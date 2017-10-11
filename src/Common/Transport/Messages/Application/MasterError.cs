using System;
using System.Collections.Generic;
using MessagePack;

namespace Ricotta.Transport.Messages.Application
{
    [MessagePackObject]
    public class MasterError
    {
        [Key(0)]
        public string ErrorMessage;
    }
}