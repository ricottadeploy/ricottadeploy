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

        public Session Session
        {
            get
            {
                return _session;
            }
        }

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

        /// <summary>
        /// Attempts to authenticate with the server.
        /// </summary>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <returns>Returns the authentication status.</returns>
        public ClientStatus TryAuthenticating(int timeout = 0)
        {
            _socket = new RequestSocket();
            _socket.Connect(_serverUri);
            var status = SendClientHello(timeout);
            if (status.HasValue)
            {
                if (status.Value == ClientStatus.Accepted)
                {
                    var result = SendClientKeyExchange(timeout);
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

        /// <summary>
        /// Sends a ClientHello message to the server. 
        /// If client public key has been accepted at the server, the server sends a ServerHello message.
        /// if client public key has been denied or the status is pending at the server, the server sends ServerAuthenticationStatus.
        /// </summary>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <returns>Returns authentication status. Returns null on timeout.</returns>
        private ClientStatus? SendClientHello(int timeout = 0)
        {
            var random = Tls12.NewRandom();
            _session.ClientRandom = random;
            var clientHello = new ClientHello
            {
                ClientId = _clientId,
                Random = random,
                RSAPublicPem = _rsa.PublicPem
            };
            var clientHelloBytes = _serializer.Serialize<ClientHello>(clientHello);
            var securityLayerMessage = new SecurityLayerMessage
            {
                Type = SecurityMessageType.ClientHello,
                Data = clientHelloBytes
            };
            var securityLayerMessageBytes = _serializer.Serialize<SecurityLayerMessage>(securityLayerMessage);
            Send(securityLayerMessageBytes);

            var receivedSecurityLayerMessageBytes = Receive(timeout);
            if (receivedSecurityLayerMessageBytes == null)
            {
                return null;
            }
            var receivedSecurityLayerMessage = _serializer.Deserialize<SecurityLayerMessage>(receivedSecurityLayerMessageBytes);
            switch (receivedSecurityLayerMessage.Type)
            {
                case SecurityMessageType.ServerAuthenticationStatus:
                    var serverAuthenticationStatus = _serializer.Deserialize<ServerAuthenticationStatus>(receivedSecurityLayerMessage.Data);
                    return serverAuthenticationStatus.Status;
                case SecurityMessageType.ServerHello:
                    var serverHello = _serializer.Deserialize<ServerHello>(receivedSecurityLayerMessage.Data);
                    _session.Id = serverHello.SessionId;
                    _session.ServerRandom = serverHello.Random;
                    _session.RSAPublicPem = serverHello.RSAPublicPem;
                    return ClientStatus.Accepted;
                default:
                    throw new Exception($"Unexpected message recieved of type {receivedSecurityLayerMessage.Type}");
            }
        }

        /// <summary>
        /// Sends a ClientKeyExchange message to the server.
        /// Server sends back a ServerFinished message.
        /// Calculates keys and updates the session.
        /// </summary>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <returns>Returns true if authentication is successful and false otherwise.</returns>
        private bool SendClientKeyExchange(int timeout)
        {
            var premasterSecret = Tls12.NewPremasterSecret();
            _session.MasterSecret = Tls12.GetMasterSecret(premasterSecret, _session.ClientRandom, _session.ServerRandom);
            var clientKeyExchange = new ClientKeyExchange
            {
                SessionId = _session.Id,
                PreMasterSecret = premasterSecret
            };
            var serverRsa = Rsa.CreateFromPublicPEM(_session.RSAPublicPem);
            var clientKeyExchangeBytes = serverRsa.Encrypt(_serializer.Serialize<ClientKeyExchange>(clientKeyExchange));
            var securityLayerMessage = new SecurityLayerMessage
            {
                Type = SecurityMessageType.ClientKeyExchange,
                Data = clientKeyExchangeBytes
            };
            var securityLayerMessageBytes = _serializer.Serialize<SecurityLayerMessage>(securityLayerMessage);
            Send(securityLayerMessageBytes);

            var receivedSecurityLayerMessageBytes = Receive(timeout);
            if (receivedSecurityLayerMessageBytes == null)
            {
                return false;
            }
            var receivedSecurityLayerMessage = _serializer.Deserialize<SecurityLayerMessage>(receivedSecurityLayerMessageBytes);
            var serverFinished = _serializer.Deserialize<ServerFinished>(receivedSecurityLayerMessage.Data);
            _session.Id = serverFinished.SessionId;
            var keys = Tls12.GetKeys(_session.MasterSecret, _session.ClientRandom, _session.ServerRandom);
            _session.ClientWriteMACKey = Tls12.GetClientWriteMACKey(keys);
            _session.ServerWriteMACKey = Tls12.GetServerWriteMACKey(keys);
            _session.ClientWriteKey = Tls12.GetClientWriteKey(keys);
            _session.ServerWriteKey = Tls12.GetServerWriteKey(keys);
            _session.PublishKey = serverFinished.PublishAesKey;
            _session.IsAuthenticated = true;
            return true;
        }

        /// <summary>
        /// Encrypts the data with client AES key and sends it to the server. 
        /// </summary>
        /// <param name="data">Byte array to be sent to the server</param>
        public void SendApplicationData(byte[] data)
        {
            if (!_session.IsAuthenticated)
            {
                throw new Exception("Not authenticated");
            }
            var aesIv = Tls12.NewAesIv();
            var applicationData = new ApplicationData
            {
                SessionId = _session.Id,
                AesIv = aesIv,
                Data = Aes.Encrypt(data, _session.ClientWriteKey, aesIv)
            };
            var applicationDataBytes = _serializer.Serialize<ApplicationData>(applicationData);
            var message = new SecurityLayerMessage
            {
                Type = SecurityMessageType.ApplicationData,
                Data = applicationDataBytes
            };
            var securityLayerMessageBytes = _serializer.Serialize<SecurityLayerMessage>(message);
            Send(securityLayerMessageBytes);
        }

        /// <summary>
        /// Receives data from the server and decrypts it with the server AES key before returning it.
        /// </summary>
        /// <returns>Returns application data sent by the server</returns>
        public byte[] ReceiveApplicationData()
        {
            if (!_session.IsAuthenticated)
            {
                throw new Exception("Not authenticated");
            }
            var applicationDataBytes = Receive();
            var applicationData = _serializer.Deserialize<ApplicationData>(applicationDataBytes);
            if (_session.Id != applicationData.SessionId)
            {
                throw new Exception("Unexpected Session ID");
            }
            var data = Aes.Decrypt(applicationData.Data, _session.ServerWriteKey, applicationData.AesIv);
            return data;
        }

        private void Send(byte[] data)
        {
            if (!_session.IsAuthenticated)
            {
                throw new Exception("Not authenticated");
            }
            _socket.SendFrame(data);
        }

        /// <summary>
        /// </summary>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <returns>Returns byte array received from socket. Returns null on timeout.</returns>
        private byte[] Receive(int timeout = 0)
        {
            byte[] bytes;
            if (timeout == 0)
            {
                bytes = _socket.ReceiveFrameBytes();
            }
            else
            {
                var timeSpan = new TimeSpan(0, 0, 0, 0, timeout);
                var recieved = _socket.TryReceiveFrameBytes(timeSpan, out bytes);
                if (!recieved)
                {
                    return null;
                }
            }
            return bytes;
        }

    }
}