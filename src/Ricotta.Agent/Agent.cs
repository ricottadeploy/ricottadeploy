using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Ricotta.Cryptography;
using Ricotta.Serialization;
using Ricotta.Transport;
using Serilog;
using Ricotta.Transport.Messages.Publish;
using Ricotta.Transport.Messages.Application;
using Newtonsoft.Json;

namespace Ricotta.Agent
{
    public class Agent : IAgent
    {
        private string _agentId;
        private readonly IConfigurationRoot _config;
        private readonly ISerializer _serializer;
        private readonly Rsa _rsa;
        private Client _client;
        private ModuleCache _moduleCache;

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
            var reqServerUrl = _config["master:request_url"];
            var interval = int.Parse(_config["authentication:interval"]);
            var intervalMs = interval * 1000;
            var maxAttempts = int.Parse(_config["authentication:max_attempts"]);
            var timeoutMs = 2000;
            _agentId = _config["id"];
            _client = new Client(_agentId, _serializer, _rsa, reqServerUrl);
            int attempt = 0;
            for (attempt = 0; attempt < maxAttempts; attempt++)
            {
                Log.Debug($"Authentication attempt {attempt + 1} of {maxAttempts} with master at {reqServerUrl}");
                var status = _client.TryAuthenticating(timeoutMs);
                if (status == ClientStatus.Denied)
                {
                    Log.Error("Master denied authentication. Exiting.");
                    Environment.Exit(0);
                }
                else if (status == ClientStatus.Accepted)
                {
                    Log.Debug("Authentication successful!");
                    break;
                }
                Thread.Sleep(intervalMs);
            }
            if (attempt == maxAttempts)
            {
                Log.Error("Maximum authentication attempts made with no success. Exiting.");
                Environment.Exit(0);
            }
            var fileRepositoryPath = _config["filerepository_path"];
            var fileRepository = new FileRepository(fileRepositoryPath, _serializer, _client);
            var moduleRepositoryPath = Path.Combine(fileRepositoryPath, "modules");
            var moduleRepository = new NuGetRepository(moduleRepositoryPath, _serializer, _client, fileRepository);
            var moduleCachePath = Path.Combine(_config["work_path"], "modules");
            _moduleCache = new ModuleCache(moduleCachePath, _serializer, _client, moduleRepository);
            Listen();
        }

        private void Listen()
        {
            var publishUrl = _config["master:publish_url"];
            var subscriber = new Subscriber(_serializer, _client.Session.PublishKey, _agentId, publishUrl);
            subscriber.SetExecuteModuleMethodHandler(HandleExecuteModuleMethod);
            Log.Debug($"Subscribing to master at {publishUrl}");
            subscriber.Listen();
        }

        private void HandleExecuteModuleMethod(ExecuteModuleMethod executeModuleMethod)
        {
            var moduleMethodString = $"{executeModuleMethod.Module}.{executeModuleMethod.Method}";
            Log.Debug(moduleMethodString);
            var moduleFullName = $"Ricotta.Modules.{executeModuleMethod.Module}";
            var moduleLoaded = _moduleCache.ModuleLoaded(moduleFullName);
            if (!moduleLoaded)
            {
                moduleLoaded = _moduleCache.LoadModule(moduleFullName);
            }
            if (moduleLoaded)
            {
                try
                {
                    var result = _moduleCache.Invoke(_agentId, executeModuleMethod.JobId, moduleFullName, executeModuleMethod.Method, executeModuleMethod.Arguments);
                    var agentJobResult = new AgentJobResult
                    {
                        AgentId = _agentId,
                        JobId = executeModuleMethod.JobId,
                        ErrorCode = 0,
                        ErrorMessage = null,
                        ResultData = JsonConvert.SerializeObject(result)
                    };
                    var agentJobResultBytes = _serializer.Serialize<AgentJobResult>(agentJobResult);
                    var applicationMessage = new ApplicationMessage
                    {
                        Type = ApplicationMessageType.AgentJobResult,
                        Data = agentJobResultBytes
                    };
                    var applicationMessageBytes = _serializer.Serialize<ApplicationMessage>(applicationMessage);
                    _client.SendApplicationData(applicationMessageBytes);
                    _client.ReceiveApplicationData();   // Ignore received message
                }
                catch (Exception e)
                {
                    Log.Error($"Error while executing {moduleMethodString}: {e.StackTrace}");
                    var agentJobResult = new AgentJobResult
                    {
                        AgentId = _agentId,
                        JobId = executeModuleMethod.JobId,
                        ErrorCode = 1,
                        ErrorMessage = e.StackTrace,
                        ResultData = null
                    };
                    var agentJobResultBytes = _serializer.Serialize<AgentJobResult>(agentJobResult);
                    var applicationMessage = new ApplicationMessage
                    {
                        Type = ApplicationMessageType.AgentJobResult,
                        Data = agentJobResultBytes
                    };
                    var applicationMessageBytes = _serializer.Serialize<ApplicationMessage>(applicationMessage);
                    _client.SendApplicationData(applicationMessageBytes);
                    _client.ReceiveApplicationData();   // Ignore recevied message
                }
            }
            else
            {
                Log.Error($"Module {executeModuleMethod.Module} does not exist or there was a problem loading it");
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