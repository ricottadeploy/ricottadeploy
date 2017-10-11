using System;

namespace Ricotta.Transport.Messages.SecurityLayer
{
    public enum SecurityMessageType
    {
        ClientHello = 0, ServerHello = 1,
        ClientKeyExchange = 2, ServerFinished = 3,
        ApplicationData = 4,
        ServerAuthenticationStatus,
        ServerError
    }
}