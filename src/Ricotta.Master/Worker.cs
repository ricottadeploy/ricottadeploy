using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ricotta.Cryptography;
using Ricotta.Serialization;
using Ricotta.Transport;
using Ricotta.Transport.Messages;
using Ricotta.Transport.Messages.Application;
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
            server.OnApplicationDataReceived(ProcessApplicationMessages);
            server.Listen();
        }

        private byte[] ProcessApplicationMessages(byte[] message)
        {
            var applicationMessage = _serializer.Deserialize<ApplicationMessage>(message);
            Log.Debug($"Recevied application message of type {applicationMessage.Type}");
            ApplicationMessage response = null;
            switch (applicationMessage.Type)
            {
                case ApplicationMessageType.AgentFileInfo:
                    response = HandleAgentFileInfo(applicationMessage);
                    break;
                case ApplicationMessageType.AgentFileChunk:
                    response = HandleAgentFileChunk(applicationMessage);
                    break;
                case ApplicationMessageType.AgentModuleInfo:
                    response = HandleAgentModuleInfo(applicationMessage);
                    break;
                case ApplicationMessageType.AgentLog:
                    break;
                case ApplicationMessageType.AgentJobStatus:
                    break;
                case ApplicationMessageType.CommandAgentList:
                    break;
                case ApplicationMessageType.CommandAgentAccept:
                    break;
                case ApplicationMessageType.CommandAgentDeny:
                    break;
                case ApplicationMessageType.CommandRunDeployment:
                    break;
            }
            return _serializer.Serialize<ApplicationMessage>(response);
        }

        private ApplicationMessage HandleAgentFileInfo(ApplicationMessage message)
        {
            var agentFileInfo = _serializer.Deserialize<AgentFileInfo>(message.Data);
            var masterFileInfo = new MasterFileInfo
            {
                Size = -1,
                IsDirectory = false,
                Sha256 = null
            };
            var response = new ApplicationMessage
            {
                Type = ApplicationMessageType.MasterFileInfo,
                Data = _serializer.Serialize<MasterFileInfo>(masterFileInfo)
            };
            return response;
        }

        private ApplicationMessage HandleAgentFileChunk(ApplicationMessage message)
        {
            var agentFileChunk = _serializer.Deserialize<AgentFileChunk>(message.Data);
            var masterFileChunk = new MasterFileChunk
            {
                Data = null
            };
            var response = new ApplicationMessage
            {
                Type = ApplicationMessageType.MasterFileChunk,
                Data = _serializer.Serialize<MasterFileChunk>(masterFileChunk)
            };
            return response;
        }

        private ApplicationMessage HandleAgentModuleInfo(ApplicationMessage message)
        {
            var agentModuleInfo = _serializer.Deserialize<AgentModuleInfo>(message.Data);
            var masterModuleInfo = new MasterModuleInfo
            {
                FileUri = null
            };
            var response = new ApplicationMessage
            {
                Type = ApplicationMessageType.MasterModuleInfo,
                Data = _serializer.Serialize<MasterModuleInfo>(masterModuleInfo)
            };
            return response;
        }

        private ApplicationMessage HandleCommandAgentDeny(ApplicationMessage message)
        {
            _clientStatusCache.Deny("fingerprint here");
            _publishAes.RegenerateKey();
            _sessionCache.Clear();
            return null;
        }
    }
}