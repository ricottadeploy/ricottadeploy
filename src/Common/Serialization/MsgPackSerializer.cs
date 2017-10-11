using MessagePack;

namespace Ricotta.Serialization
{
    public class MsgPackSerializer : ISerializer
    {
        public T Deserialize<T>(byte[] message)
        {
            return MessagePackSerializer.Deserialize<T>(message);
        }

        public byte[] Serialize<T>(T message)
        {
            return MessagePackSerializer.Serialize(message);
        }
    }
}