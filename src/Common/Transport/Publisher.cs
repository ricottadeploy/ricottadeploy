using NetMQ;
using NetMQ.Sockets;
using Ricotta.Cryptography;
using Ricotta.Serialization;
using Ricotta.Transport.Messages.Publish;

namespace Ricotta.Transport
{
    public class Publisher
    {
        private ISerializer _serializer;
        private Aes _aes;
        private PublisherSocket _socket;

        public Aes Aes
        {
            get
            {
                return _aes;
            }
        }

        public Publisher(ISerializer serializer,
                            Aes aes,
                            string publishUri)
        {
            _serializer = serializer;
            _aes = aes;
            _socket = new PublisherSocket();
            _socket.Bind(publishUri);
        }

        public void SendExecuteModuleMethod(string selector,
                                            byte[] aesIv,
                                            string module,
                                            string method,
                                            object[] arguments)
        {
            var executeModuleMethod = new ExecuteModuleMethod
            {
                Module = module,
                Method = method,
                Arguments = arguments
            };
            var publishMessage = new PublishMessage
            {
                Selector = selector,
                AesIv = aesIv,
                Type = PublishMessageType.ExecuteModuleMethod,
                Data = Aes.Encrypt(_serializer.Serialize<ExecuteModuleMethod>(executeModuleMethod), _aes.Key, aesIv)
            };
            var bytes = _serializer.Serialize<PublishMessage>(publishMessage);
            Send(bytes);
        }

        private void Send(byte[] data)
        {
            _socket.SendFrame(data);
        }

    }
}