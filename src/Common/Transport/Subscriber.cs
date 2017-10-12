using System;
using NetMQ;
using NetMQ.Sockets;
using Ricotta.Cryptography;
using Ricotta.Serialization;
using Ricotta.Transport.Messages.Publish;

namespace Ricotta.Transport
{
    public class Subscriber
    {
        public delegate void ExecuteModuleMethodHandler(ExecuteModuleMethod message);
        private ExecuteModuleMethodHandler _executeModuleMethodHandler;

        private ISerializer _serializer;
        private byte[] _aesKey;
        private SubscriberSocket _socket;
        private string _clientId;

        public Subscriber(ISerializer serializer,
                            byte[] aesKey,
                            string clientId,
                            string publishUri)
        {
            _serializer = serializer;
            _aesKey = aesKey;
            _socket = new SubscriberSocket();
            _socket.Connect(publishUri);
            _socket.SubscribeToAnyTopic();
            _executeModuleMethodHandler = new ExecuteModuleMethodHandler(DefaultExecuteModuleMethodHandler);
        }

        public void Listen()
        {
            while (true)
            {
                var publishMessageBytes = Receive();
                var publishMessage = _serializer.Deserialize<PublishMessage>(publishMessageBytes);
                if (publishMessage.Selector == "*" || publishMessage.Selector == _clientId)
                {
                    var decryptedMessageBytes = Aes.Decrypt(publishMessage.Data, _aesKey, publishMessage.AesIv);
                    if (publishMessage.Type == PublishMessageType.ExecuteModuleMethod)
                    {
                        var executeModuleMethod = _serializer.Deserialize<ExecuteModuleMethod>(decryptedMessageBytes);
                        _executeModuleMethodHandler(executeModuleMethod);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        private byte[] Receive()
        {
            return _socket.ReceiveFrameBytes();
        }

        private void DefaultExecuteModuleMethodHandler(ExecuteModuleMethod message)
        {
        }

        public void SetExecuteModuleMethodHandler(Action<ExecuteModuleMethod> executeModuleMethodHandler)
        {
            _executeModuleMethodHandler = new ExecuteModuleMethodHandler(executeModuleMethodHandler);
        }

    }
}