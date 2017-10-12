using System.IO;
using System.Threading;
using Microsoft.Extensions.Configuration;
using NetMQ;
using NetMQ.Sockets;
using Serilog;
using Ricotta.Cryptography;
using Ricotta.Serialization;
using Ricotta.Transport;

namespace Ricotta.Master
{
    public class Master : IMaster
    {
        private const string WORKERS_URL = "inproc://workers";
        private readonly IConfigurationRoot _config;
        private readonly ISerializer _serializer;
        private readonly Rsa _rsa;
        private Aes _publishAes;
        private Publisher _publisher;
        private SessionCache _sessionCache;
        private ClientAuthInfoCache _clientAuthInfoCache;
        private FileRepository _fileRepository;

        public Master(IConfigurationRoot config,
                        ISerializer serializer)
        {
            Log.Information($"Ricotta Master");
            _config = config;
            _serializer = serializer;
            _rsa = GetRsaKeys();
            _publishAes = Aes.Create();
            _sessionCache = new SessionCache();
            _clientAuthInfoCache = new ClientAuthInfoCache();
            _fileRepository = new FileRepository(_config["filerepository_path"]);
            LoadPreAcceptedAgents();
        }

        private void LoadPreAcceptedAgents()
        {
            var keysPath = _config["keys_path"];
            var agentKeysPath = Path.Combine(keysPath, "agent");
            var agentPublicKeyFiles = Directory.GetFiles(agentKeysPath, "*.pem");
            foreach (var agentPublicKeyFile in agentPublicKeyFiles)
            {
                var agentId = Path.GetFileNameWithoutExtension(agentPublicKeyFile);
                var agentPublicPem = File.ReadAllText(agentPublicKeyFile);
                var agentRsa = Rsa.CreateFromPublicPEM(agentPublicPem);
                _clientAuthInfoCache.Add(agentRsa.Fingerprint, agentId, ClientStatus.Accepted);
                Log.Information($"Trusted agent {agentId} with RSA fingerprint {agentRsa.Fingerprint}");
            }
        }

        public void Start()
        {
            int workerId = 0;
            int workers_count = int.Parse(_config["workers"]);
            var request_url = _config["bind:request_url"];
            var publish_url = _config["bind:publish_url"];
            Log.Information($"Starting publish server at {publish_url}");
            _publisher = new Publisher(_serializer, _publishAes, _config["bind:publish_url"]);

            using (var clients = new RouterSocket())
            {
                using (var workers = new DealerSocket())
                {
                    Log.Information($"Starting request server at {request_url}");
                    clients.Bind(request_url);
                    workers.Bind(WORKERS_URL);

                    for (int i = 0; i < workers_count; i++)
                    {
                        new Thread(() => new Worker(workerId++,
                                                WORKERS_URL,
                                                _serializer,
                                                _rsa,
                                                _publisher,
                                                _sessionCache,
                                                _clientAuthInfoCache,
                                                _fileRepository)).Start();
                    }
                    new Thread(() => new TestThread(_publisher)).Start();
                    var proxy = new Proxy(clients, workers);
                    proxy.Start();
                }
            }
        }

        private Rsa GetRsaKeys()
        {
            Rsa rsa = null;
            var keysPath = _config["keys_path"];
            var privatePemFilename = Path.Combine(keysPath, "master", "private.pem");
            var publicPemFilename = Path.Combine(keysPath, "master", "public.pem");
            if (File.Exists(privatePemFilename))
            {
                Log.Information($"Loading RSA keys from {privatePemFilename}");
                var privatePem = File.ReadAllText(privatePemFilename);
                rsa = Rsa.CreateFromPrivatePEM(privatePem);
            }
            else
            {
                Log.Information($"Generating RSA keys");
                rsa = Rsa.Create();
                File.WriteAllText(privatePemFilename, rsa.PrivatePem);
                File.WriteAllText(publicPemFilename, rsa.PublicPem);
            }
            Log.Information($"RSA fingerprint is {rsa.Fingerprint}");
            return rsa;
        }

    }
}