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

        public void SendExecuteModuleMethod(string environment,
                                            string selector,
                                            byte[] aesIv,
                                            string jobId,
                                            string module,
                                            string method,
                                            object[] arguments)
        {
            var executeModuleMethod = new ExecuteModuleMethod
            {
                JobId = jobId,
                Module = module,
                Method = method,
                Arguments = arguments
            };
            var executeModuleMethodBytes = _serializer.Serialize<ExecuteModuleMethod>(executeModuleMethod);
            var encryptedExecuteModuleMethodBytes = Aes.Encrypt(executeModuleMethodBytes, _aes.Key, aesIv);
            var publishMessage = new PublishMessage
            {
                Environment = environment,
                Selector = selector,
                AesIv = aesIv,
                Type = PublishMessageType.ExecuteModuleMethod,
                Data = encryptedExecuteModuleMethodBytes
            };
            var publishMessageBytes = _serializer.Serialize<PublishMessage>(publishMessage);
            Send(publishMessageBytes);
        }

        private void Send(byte[] data)
        {
            _socket.SendFrame(data);
        }

    }
}