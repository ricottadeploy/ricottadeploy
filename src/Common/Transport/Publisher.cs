using System;
using NetMQ;
using Ricotta.Cryptography;
using Ricotta.Serialization;
using Ricotta.Transport.Messages.SecurityLayer;
using Ricotta.Transport;
using NetMQ.Sockets;
using Serilog;
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
                Data = EncryptAes(_serializer.Serialize<ExecuteModuleMethod>(executeModuleMethod), _aes.Key, aesIv)
            };
            var bytes = _serializer.Serialize<PublishMessage>(publishMessage);
            Send(bytes);
        }

        private void Send(byte[] data)
        {
            _socket.SendFrame(data);
        }

        private byte[] EncryptAes(byte[] data, byte[] key, byte[] iv)
        {
            var aes = Aes.Create(key, iv);
            return aes.Encrypt(data);
        }

    }
}