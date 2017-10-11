using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ricotta.Cryptography;
using Ricotta.Serialization;
using Ricotta.Transport;
using Ricotta.Transport.Messages;
using Serilog;

namespace Ricotta.Master
{
    public class Worker
    {
        private readonly int _workerId;
        private readonly string _workersUrl;
        private readonly ISerializer _serializer;
        private readonly Rsa _rsa;
        private Aes _publishAes;
        private readonly SessionCache _sessionCache;
        private readonly ClientStatusCache _clientStatusCache;

        public Worker(int workerId,
                        string workersUrl,
                        ISerializer serializer,
                        Rsa rsa,
                        Aes publishAes,
                        SessionCache sessionCache,
                        ClientStatusCache clientStatusCache)
        {
            _workerId = workerId;
            _workersUrl = workersUrl;
            _serializer = serializer;
            _rsa = rsa;
            _publishAes = publishAes;
            _sessionCache = sessionCache;
            _clientStatusCache = clientStatusCache;
            Run();
        }

        private void Run()
        {
            Log.Debug($"Started Worker {_workerId}");
            var server = new Server(_serializer, _rsa, _sessionCache, _workersUrl);
            server.OnApplicationDataReceived(data =>
            {
                foreach (var b in data)
                {
                    Log.Debug($"{b}");
                }
                return new byte[] { 5, 4, 3 };
            });
            server.Listen();
        }
    }
}