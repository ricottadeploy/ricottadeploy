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

        public Subscriber(ISerializer serializer,
                            byte[] aesKey,
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
                var messageBytes = Receive();
                var message = _serializer.Deserialize<PublishMessage>(messageBytes);
                // TODO: Check Selector to see if message is meant for this subscriber 
                var decryptedMessageData = Aes.Decrypt(message.Data, _aesKey, message.AesIv);
                if (message.Type == PublishMessageType.ExecuteModuleMethod)
                {
                    var executeModuleMethod = _serializer.Deserialize<ExecuteModuleMethod>(decryptedMessageData);
                    _executeModuleMethodHandler(executeModuleMethod);
                }
            }
        }

        private byte[] Receive()
        {
            return _socket.ReceiveFrameBytes();
        }

        public void DefaultExecuteModuleMethodHandler(ExecuteModuleMethod message)
        {
        }

        public void SetExecuteModuleMethodHandler(Action<ExecuteModuleMethod> executeModuleMethodHandler)
        {
            _executeModuleMethodHandler = new ExecuteModuleMethodHandler(executeModuleMethodHandler);
        }

    }
}