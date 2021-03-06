using System;
using NetMQ;
using Ricotta.Cryptography;
using Ricotta.Serialization;
using Ricotta.Transport.Messages.SecurityLayer;
using Ricotta.Transport;
using NetMQ.Sockets;

namespace Ricotta.Transport
{
    public class Server
    {
        private NetMQSocket _socket;
        private ISerializer _serializer;
        private SessionCache _sessionCache;
        private Rsa _rsa;

        public Server(ISerializer serializer, string serverUri)
        {
            _serializer = serializer;
            _rsa = Rsa.Create();
            _sessionCache = new SessionCache();
            _socket = new ResponseSocket();
            _socket.Bind(serverUri);

            while (true)
            {
                var messageBytes = _socket.ReceiveFrameBytes();
                var message = _serializer.Deserialize<SecurityLayerMessage>(messageBytes);
                switch (message.Type)
                {
                    case SecurityMessageType.ClientHello:
                        HandleClientHello(message.Data);
                        break;
                    case SecurityMessageType.ClientKeyExchange:
                        HandleClientKeyExchange(message.Data);
                        break;
                    case SecurityMessageType.ApplicationData:
                        HandleApplicationData(message.Data);
                        break;
                }
            }
        }

        private void Send(byte[] data)
        {
            _socket.SendFrame(data);
        }

        private byte[] Receive()
        {
            return _socket.ReceiveFrameBytes();
        }

        private void HandleClientHello(byte[] message)
        {
            var clientHello = _serializer.Deserialize<ClientHello>(message);
            var session = _sessionCache.NewSession();
            var random = TLS12.GetRandom();
            session.ServerRandom = random;
            session.ClientRandom = clientHello.Random;
            session.RSAPublicPem = clientHello.RSAPublicPem;
            var serverHello = new ServerHello
            {
                SessionId = session.Id,
                Random = random,
                RSAPublicPem = _rsa.PublicPem
            };
            var response = new SecurityLayerMessage
            {
                Type = SecurityMessageType.ServerHello,
                Data = _serializer.Serialize<ServerHello>(serverHello)
            };
            var responseBytes = _serializer.Serialize<SecurityLayerMessage>(response);
            Send(responseBytes);
        }

        private void HandleClientKeyExchange(byte[] message)
        {
            var decryptedMessage = _rsa.Decrypt(message);
            var clientKeyExchange = _serializer.Deserialize<ClientKeyExchange>(decryptedMessage);
            var session = _sessionCache.Get(clientKeyExchange.SessionId);
            if (session == null)
            {
                // TODO: Send error
            }
            else
            {
                session.MasterSecret = TLS12.GetMasterSecret(clientKeyExchange.PreMasterSecret, session.ClientRandom, session.ServerRandom);
                var keys = TLS12.GetKeys(session.MasterSecret, session.ClientRandom, session.ServerRandom);
                session.ClientWriteMACKey = TLS12.GetClientWriteMACKey(keys);
                session.ServerWriteMACKey = TLS12.GetServerWriteMACKey(keys);
                session.ClientWriteKey = TLS12.GetClientWriteKey(keys);
                session.ServerWriteKey = TLS12.GetServerWriteKey(keys);
                session.IsAuthenticated = true;
            }
            var serverFinished = new ServerFinished
            {
                SessionId = session.Id
            };
            var response = new SecurityLayerMessage
            {
                Type = SecurityMessageType.ServerFinished,
                Data = _serializer.Serialize<ServerFinished>(serverFinished)
            };
            var responseBytes = _serializer.Serialize<SecurityLayerMessage>(response);
            Send(responseBytes);
        }

        private void HandleApplicationData(byte[] message)
        {
            var applicationData = _serializer.Deserialize<ApplicationData>(message);
            var session = _sessionCache.Get(applicationData.SessionId);
            if (!session.IsAuthenticated)
            {
                // TODO: Send error
            }
            else
            {
                var data = DecryptAes(applicationData.Data, session.ClientWriteKey, applicationData.AesIv);
                var responseData = ProcessApplicationData(data);
                var aesIv = TLS12.GetIV();
                var applicationDataResponse = new ApplicationData
                {
                    SessionId = session.Id,
                    AesIv = aesIv,
                    Data = EncryptAes(responseData, session.ServerWriteKey, aesIv)
                };
                var bytes = _serializer.Serialize<ApplicationData>(applicationDataResponse);
                Send(bytes);
            }
        }

        private byte[] EncryptAes(byte[] data, byte[] key, byte[] iv)
        {
            var aes = Aes.Create(key, iv);
            return aes.Encrypt(data);
        }

        private byte[] DecryptAes(byte[] data, byte[] key, byte[] iv)
        {
            var aes = Aes.Create(key, iv);
            return aes.Decrypt(data);
        }

        public byte[] ProcessApplicationData(byte[] message)
        {
            foreach (var b in message)
            {
                Console.WriteLine(b);
            }
            return new byte[] { 1, 3, 5 };
        }
    }
}