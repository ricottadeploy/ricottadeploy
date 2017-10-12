using System;
using NetMQ;
using Ricotta.Cryptography;
using Ricotta.Serialization;
using Ricotta.Transport.Messages.SecurityLayer;
using Ricotta.Transport;
using NetMQ.Sockets;
using Serilog;

namespace Ricotta.Transport
{
    public class Server
    {
        private const string CLI_CLIENT_ID = "!";
        public delegate byte[] OnApplicationDataReceivedCallback(byte[] data);
        private OnApplicationDataReceivedCallback _onApplicationDataReceivedCallback;
        private ISerializer _serializer;
        private Rsa _rsa;
        private Publisher _publisher;
        private NetMQSocket _socket;
        private readonly SessionCache _sessionCache;
        private readonly ClientAuthInfoCache _clientAuthInfoCache;

        public Server(ISerializer serializer,
                        Rsa rsa,
                        Publisher publisher,
                        SessionCache sessionCache,
                        ClientAuthInfoCache clientAuthInfoCache,
                        string serverUri)
        {
            _onApplicationDataReceivedCallback = new OnApplicationDataReceivedCallback(DefaultOnApplicationDataReceivedCallback);
            _serializer = serializer;
            _rsa = rsa;
            _publisher = publisher;
            _sessionCache = sessionCache;
            _clientAuthInfoCache = clientAuthInfoCache;
            // Always accept requests coming from CLI. CLI uses master's RSA keys so add its fingerprint as accepted.
            _clientAuthInfoCache.Add(_rsa.Fingerprint, "!", ClientStatus.Accepted);
            _socket = new ResponseSocket();
            _socket.Connect(serverUri);
        }

        /// <summary>
        /// Listens to messages from clients, handles them and sends the response back.
        /// </summary>
        public void Listen()
        {
            while (true)
            {
                var securityLayerMessageBytes = _socket.ReceiveFrameBytes();
                var securityLayerMessage = _serializer.Deserialize<SecurityLayerMessage>(securityLayerMessageBytes);
                byte[] responseSecurityLayerMessageBytes = null;
                switch (securityLayerMessage.Type)
                {
                    case SecurityMessageType.ClientHello:
                        responseSecurityLayerMessageBytes = HandleClientHello(securityLayerMessage.Data);
                        break;
                    case SecurityMessageType.ClientKeyExchange:
                        responseSecurityLayerMessageBytes = HandleClientKeyExchange(securityLayerMessage.Data);
                        break;
                    case SecurityMessageType.ApplicationData:
                        responseSecurityLayerMessageBytes = HandleApplicationData(securityLayerMessage.Data);
                        break;
                }
                Send(responseSecurityLayerMessageBytes);
            }
        }

        /// <summary>
        /// Handles ClientHello message from the client and responds with ServerHello message.
        /// Sends a ServerError message on error. 
        /// </summary>
        /// <param name="clientHelloMessageBytes"></param>
        /// <returns></returns>
        private byte[] HandleClientHello(byte[] clientHelloMessageBytes)
        {
            var clientHello = _serializer.Deserialize<ClientHello>(clientHelloMessageBytes);
            var clientRsa = Rsa.CreateFromPublicPEM(clientHello.RSAPublicPem);
            Log.Debug($"Received ClientHello from {clientHello.ClientId} with RSA fingerprint {clientRsa.Fingerprint}");
            var clientAuthInfo = _clientAuthInfoCache.Get(clientRsa.Fingerprint);
            if (clientAuthInfo == null)
            {
                clientAuthInfo = _clientAuthInfoCache.Add(clientRsa.Fingerprint, clientHello.ClientId, ClientStatus.Pending);
            }
            Log.Debug($"{clientHello.ClientId} authentication status: {clientAuthInfo.AuthenticationStatus}");
            if (clientAuthInfo.AuthenticationStatus == ClientStatus.Accepted)
            {
                Session session = null;
                if (clientHello.ClientId == CLI_CLIENT_ID)
                {
                    session = _sessionCache.NewCliSession();
                }
                else
                {
                    session = _sessionCache.NewSession();
                }
                var random = Tls12.NewRandom();
                session.ServerRandom = random;
                session.ClientRandom = clientHello.Random;
                session.RSAPublicPem = clientHello.RSAPublicPem;
                var serverHello = new ServerHello
                {
                    SessionId = session.Id,
                    Random = random,
                    RSAPublicPem = _rsa.PublicPem
                };
                var serverHelloBytes = _serializer.Serialize<ServerHello>(serverHello);
                var securityLayerMessage = new SecurityLayerMessage
                {
                    Type = SecurityMessageType.ServerHello,
                    Data = serverHelloBytes
                };
                var securityLayerMessageBytes = _serializer.Serialize<SecurityLayerMessage>(securityLayerMessage);
                return securityLayerMessageBytes;
            }
            else
            {
                var serverAuthenticationStatus = new ServerAuthenticationStatus
                {
                    Status = clientAuthInfo.AuthenticationStatus
                };
                var serverAuthenticationStatusBytes = _serializer.Serialize<ServerAuthenticationStatus>(serverAuthenticationStatus);
                var securityLayerMessage = new SecurityLayerMessage
                {
                    Type = SecurityMessageType.ServerAuthenticationStatus,
                    Data = serverAuthenticationStatusBytes
                };
                var securityLayerMessageBytes = _serializer.Serialize<SecurityLayerMessage>(securityLayerMessage);
                return securityLayerMessageBytes;
            }
        }

        /// <summary>
        /// Handles ClientKeyExchange message from the client and responds with ServerFinished message.
        /// Sends a ServerError message on error. 
        /// </summary>
        /// <param name="encryptedClientKeyExchangeBytes"></param>
        /// <returns></returns>
        private byte[] HandleClientKeyExchange(byte[] encryptedClientKeyExchangeBytes)
        {
            var decryptedClientKeyExchangeBytes = _rsa.Decrypt(encryptedClientKeyExchangeBytes);
            var clientKeyExchange = _serializer.Deserialize<ClientKeyExchange>(decryptedClientKeyExchangeBytes);
            var session = _sessionCache.Get(clientKeyExchange.SessionId);
            if (session == null)
            {
                return GetServerError("Invalid Session ID");
            }
            else
            {
                session.MasterSecret = Tls12.GetMasterSecret(clientKeyExchange.PreMasterSecret, session.ClientRandom, session.ServerRandom);
                var keys = Tls12.GetKeys(session.MasterSecret, session.ClientRandom, session.ServerRandom);
                session.ClientWriteMACKey = Tls12.GetClientWriteMACKey(keys);
                session.ServerWriteMACKey = Tls12.GetServerWriteMACKey(keys);
                session.ClientWriteKey = Tls12.GetClientWriteKey(keys);
                session.ServerWriteKey = Tls12.GetServerWriteKey(keys);
                session.IsAuthenticated = true;
            }
            var serverFinished = new ServerFinished
            {
                SessionId = session.Id,
                PublishAesKey = _publisher.Aes.Key
            };
            var serverFinishedBytes = _serializer.Serialize<ServerFinished>(serverFinished);
            var securityLayerMessage = new SecurityLayerMessage
            {
                Type = SecurityMessageType.ServerFinished,
                Data = serverFinishedBytes
            };
            var securityLayerMessageBytes = _serializer.Serialize<SecurityLayerMessage>(securityLayerMessage);
            return securityLayerMessageBytes;
        }

        /// <summary>
        /// Handles ApplicationData message from the client and sends response ApplicationData message.
        /// </summary>
        /// <param name="applicationDataBytes"></param>
        /// <returns></returns>
        private byte[] HandleApplicationData(byte[] applicationDataBytes)
        {
            var applicationData = _serializer.Deserialize<ApplicationData>(applicationDataBytes);
            var session = _sessionCache.Get(applicationData.SessionId);
            if (!session.IsAuthenticated)
            {
                return GetServerError("Not authenticated");
            }
            else
            {
                var data = Aes.Decrypt(applicationData.Data, session.ClientWriteKey, applicationData.AesIv);
                var responseData = _onApplicationDataReceivedCallback(data);
                var aesIv = Tls12.NewAesIv();
                var encryptedResponseData = Aes.Encrypt(responseData, session.ServerWriteKey, aesIv);
                var responseApplicationData = new ApplicationData
                {
                    SessionId = session.Id,
                    AesIv = aesIv,
                    Data = encryptedResponseData
                };
                var responseApplicationDataBytes = _serializer.Serialize<ApplicationData>(responseApplicationData);
                if (_sessionCache.IsCliSession(session.Id))
                {
                    _sessionCache.Destroy(session.Id);
                }
                return responseApplicationDataBytes;
            }
        }

        private byte[] DefaultOnApplicationDataReceivedCallback(byte[] message)
        {
            return new byte[] { };
        }

        /// <summary>
        /// Sets callback when application data message is received.
        /// </summary>
        /// <param name="onApplicationDataReceivedCallback"></param>
        public void OnApplicationDataReceived(Func<byte[], byte[]> onApplicationDataReceivedCallback)
        {
            _onApplicationDataReceivedCallback = new OnApplicationDataReceivedCallback(onApplicationDataReceivedCallback);
        }

        /// <summary>
        /// Creates a ServerError message with the error message set
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <returns>Returns serialized ServerError</returns>
        private byte[] GetServerError(string errorMessage)
        {
            var serverError = new ServerError
            {
                ErrorMessage = errorMessage
            };
            var serverErrorBytes = _serializer.Serialize<ServerError>(serverError);
            var securityLayerMessage = new SecurityLayerMessage
            {
                Type = SecurityMessageType.ServerError,
                Data = serverErrorBytes
            };
            var securityLayerMessageBytes = _serializer.Serialize<SecurityLayerMessage>(securityLayerMessage);
            return securityLayerMessageBytes;
        }

        private void Send(byte[] data)
        {
            _socket.SendFrame(data);
        }

        private byte[] Receive()
        {
            return _socket.ReceiveFrameBytes();
        }

    }
}