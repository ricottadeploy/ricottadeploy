using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Ricotta.Cryptography;
using Ricotta.Serialization;
using Ricotta.Transport;
using Serilog;

namespace Ricotta.Agent
{
    public class Agent : IAgent
    {
        private string _agentId;
        private readonly IConfigurationRoot _config;
        private readonly ISerializer _serializer;
        private readonly Rsa _rsa;

        public Agent(IConfigurationRoot config,
                        ISerializer serializer)
        {
            Log.Information($"Ricotta Agent");
            _config = config;
            _serializer = serializer;
            _rsa = GetRsaKeys();
        }

        public void Start()
        {
            _agentId = _config["id"];
            var reqServerUrl = _config["master:request_url"];
            var client = new Client(_agentId, _serializer, _rsa, reqServerUrl);
            var interval = int.Parse(_config["authentication:interval"]);
            var intervalMs = interval * 1000;
            var maxAttempts = int.Parse(_config["authentication:max_attempts"]);
            var timeoutMs = 2000;
            int attempt = 0;
            for (attempt = 0; attempt < maxAttempts; attempt++)
            {
                Log.Debug($"Authentication attempt {attempt + 1} of {maxAttempts} with master at {reqServerUrl}");
                var status = client.TryAuthenticating(timeoutMs);
                if (status == ClientStatus.Denied)
                {
                    Log.Error("Master denied authentication. Exiting.");
                    Environment.Exit(0);
                }
                else if (status == ClientStatus.Accepted)
                {
                    break;
                }
                Thread.Sleep(intervalMs);
            }
            if (attempt == maxAttempts)
            {
                Log.Error("Maxium authentication attempts made with no success. Exiting.");
                Environment.Exit(0);
            }
            Log.Debug("Authentication successful");

            // client.SendApplicationData(new byte[] { 5, 6, 7 });
            // var data = client.ReceiveApplicationData();
            // foreach (var b in data)
            // {
            //     Log.Debug($"{b}");
            // }
        }

        private Rsa GetRsaKeys()
        {
            Rsa rsa = null;
            var keysPath = _config["keys_path"];
            var privatePemFilename = Path.Combine(keysPath, "agent", "private.pem");
            var publicPemFilename = Path.Combine(keysPath, "agent", "public.pem");
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