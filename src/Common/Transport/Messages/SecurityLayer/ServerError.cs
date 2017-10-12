using System;
using MessagePack;

namespace Ricotta.Transport.Messages.SecurityLayer
{
    [MessagePackObject]
    public class ServerError
    {
        [Key(0)]
        public string ErrorMessage;
    }
}