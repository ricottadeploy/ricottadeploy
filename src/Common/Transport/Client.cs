using System;
using NetMQ;
using NetMQ.Sockets;
using Ricotta.Serialization;
using Ricotta.Transport.Messages.SecurityLayer;
using Ricotta.Cryptography;

namespace Ricotta.Transport
{
    public class Client
    {
        private string _clientId;
        private ISerializer _serializer;
        private Rsa _rsa;
        private string _serverUri;
        private NetMQSocket _socket;
        private Session _session;

        public Client(string clientId,
                        ISerializer serializer,
                        Rsa rsa,
                        string serverUri)
        {
            _clientId = clientId;
            _serializer = serializer;
            _rsa = rsa;
            _serverUri = serverUri;
            _session = new Session() { IsAuthenticated = false };
        }

        public bool Authenticated
        {
            get
            {
                return _session.IsAuthenticated;
            }
        }

        public ClientStatus TryAuthenticating(int milliseconds = 0)
        {
            _socket = new RequestSocket();
            _socket.Connect(_serverUri);
            var status = SendClientHello(milliseconds);
            if (status.HasValue)
            {
                if (status.Value == ClientStatus.Accepted)
                {
                    var result = SendClientKeyExchange(milliseconds);
                    return ClientStatus.Accepted;
                }
                else
                {
                    return status.Value;
                }
            }
            else
            {
                return ClientStatus.Pending;
            }
        }

        private ClientStatus? SendClientHello(int milliseconds = 0)
        {
            var random = TLS12.GetRandom();
            _session.ClientRandom = random;
            var clientHello = new ClientHello
            {
                ClientId = _clientId,
                Random = random,
                RSAPublicPem = _rsa.PublicPem
            };
            var request = new SecurityLayerMessage
            {
                Type = SecurityMessageType.ClientHello,
                Data = _serializer.Serialize<ClientHello>(clientHello)
            };
            var requestBytes = _serializer.Serialize<SecurityLayerMessage>(request);
            Send(requestBytes);

            var responseBytes = Receive(milliseconds);
            if (responseBytes == null)
            {
                return null;
            }
            var message = _serializer.Deserialize<SecurityLayerMessage>(responseBytes);
            switch (message.Type)
            {
                case SecurityMessageType.ServerAuthenticationStatus:
                    var serverAuthenticationStatus = _serializer.Deserialize<ServerAuthenticationStatus>(message.Data);
                    return serverAuthenticationStatus.Status;
                case SecurityMessageType.ServerHello:
                    var serverHello = _serializer.Deserialize<ServerHello>(message.Data);
                    _session.Id = serverHello.SessionId;
                    _session.ServerRandom = serverHello.Random;
                    _session.RSAPublicPem = serverHello.RSAPublicPem;
                    return ClientStatus.Accepted;
                default:
                    throw new Exception($"Unexpected message recieved of type {message.Type}");
            }
        }

        private bool SendClientKeyExchange(int milliseconds)
        {
            var preMasterSecret = TLS12.GetPreMasterSecret();
            _session.MasterSecret = TLS12.GetMasterSecret(preMasterSecret, _session.ClientRandom, _session.ServerRandom);
            var clientKeyExchange = new ClientKeyExchange
            {
                SessionId = _session.Id,
                PreMasterSecret = preMasterSecret
            };
            var serverRsa = Rsa.CreateFromPublicPEM(_session.RSAPublicPem);
            var request = new SecurityLayerMessage
            {
                Type = SecurityMessageType.ClientKeyExchange,
                Data = serverRsa.Encrypt(_serializer.Serialize<ClientKeyExchange>(clientKeyExchange))
            };
            var requestBytes = _serializer.Serialize<SecurityLayerMessage>(request);
            Send(requestBytes);

            var responseBytes = Receive(milliseconds);
            if (responseBytes == null) return false;

            var message = _serializer.Deserialize<SecurityLayerMessage>(responseBytes);
            var serverFinished = _serializer.Deserialize<ServerFinished>(message.Data);

            _session.Id = serverFinished.SessionId;
            var keys = TLS12.GetKeys(_session.MasterSecret, _session.ClientRandom, _session.ServerRandom);
            _session.ClientWriteMACKey = TLS12.GetClientWriteMACKey(keys);
            _session.ServerWriteMACKey = TLS12.GetServerWriteMACKey(keys);
            _session.ClientWriteKey = TLS12.GetClientWriteKey(keys);
            _session.ServerWriteKey = TLS12.GetServerWriteKey(keys);
            _session.IsAuthenticated = true;

            return true;
        }

        private void Send(byte[] data)
        {
            _socket.SendFrame(data);
        }

        private byte[] Receive(int milliseconds = 0)
        {
            byte[] bytes;
            if (milliseconds == 0)
            {
                bytes = _socket.ReceiveFrameBytes();
            }
            else
            {
                var recieved = _socket.TryReceiveFrameBytes(new TimeSpan(0, 0, 0, 0, milliseconds), out bytes);
                if (!recieved) return null;
            }
            return bytes;
        }

        public void SendApplicationData(byte[] data)
        {
            if (!_session.IsAuthenticated)
            {
                throw new Exception("Not authenticated");
            }
            var aesIv = TLS12.GetIV();
            var applicationData = new ApplicationData
            {
                SessionId = _session.Id,
                AesIv = aesIv,
                Data = EncryptAes(data, _session.ClientWriteKey, aesIv)
            };
            var bytes = _serializer.Serialize<ApplicationData>(applicationData);
            var message = new SecurityLayerMessage
            {
                Type = SecurityMessageType.ApplicationData,
                Data = bytes
            };
            Send(_serializer.Serialize<SecurityLayerMessage>(message));
        }

        public byte[] ReceiveApplicationData()
        {
            if (!_session.IsAuthenticated)
            {
                throw new Exception("Not authenticated");
            }
            var message = Receive();
            var applicationData = _serializer.Deserialize<ApplicationData>(message);
            if (_session.Id != applicationData.SessionId)
            {
                // Something is wrong. Handle it!
            }
            return DecryptAes(applicationData.Data, _session.ServerWriteKey, applicationData.AesIv);
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

    }
}