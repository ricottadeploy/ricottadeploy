
namespace Ricotta.Serialization
{
    public interface ISerializer
    {
        byte[] Serialize<T>(T message);
        T Deserialize<T>(byte[] message);
    }
}