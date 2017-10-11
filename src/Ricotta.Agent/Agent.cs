using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Ricotta.Cryptography;
using Ricotta.Serialization;
using Ricotta.Transport;
using Serilog;

namespace Ricotta.Agent
{
    public class Agent : IAgent
    {
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
            var client = new Client(_serializer, _rsa);
            client.Connect(_config["master:request_url"]);
            client.SendApplicationData(new byte[] { 5, 6, 7 });
            var data = client.ReceiveApplicationData();
            foreach(var b in data)
            {
                Log.Debug($"{b}");
            }
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