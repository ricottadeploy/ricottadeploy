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
        private Publisher _publisher;
        private readonly SessionCache _sessionCache;
        private readonly ClientAuthInfoCache _clientAuthInfoCache;
        private readonly FileRepository _fileRepository;

        public Worker(int workerId,
                        string workersUrl,
                        ISerializer serializer,
                        Rsa rsa,
                        Publisher publisher,
                        SessionCache sessionCache,
                        ClientAuthInfoCache clientAuthInfoCache,
                        FileRepository fileRepository)
        {
            _workerId = workerId;
            _workersUrl = workersUrl;
            _serializer = serializer;
            _rsa = rsa;
            _publisher = publisher;
            _sessionCache = sessionCache;
            _clientAuthInfoCache = clientAuthInfoCache;
            _fileRepository = fileRepository;
            Run();
        }

        private void Run()
        {
            Log.Debug($"Started Worker {_workerId}");
            var server = new Server(_serializer, _rsa, _publisher, _sessionCache, _clientAuthInfoCache, _workersUrl);
            server.OnApplicationDataReceived(ProcessApplicationMessages);
            server.Listen();
        }

        private byte[] ProcessApplicationMessages(byte[] message)
        {
            var applicationMessage = _serializer.Deserialize<ApplicationMessage>(message);
            Log.Debug($"Received application message of type {applicationMessage.Type}");
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
                    // TODO
                    break;
                case ApplicationMessageType.AgentJobStatus:
                    // TODO
                    break;
                case ApplicationMessageType.CommandAgentList:
                    response = HandleCommandAgentList(applicationMessage);
                    break;
                case ApplicationMessageType.CommandAgentAccept:
                    response = HandleCommandAgentAccept(applicationMessage);
                    break;
                case ApplicationMessageType.CommandAgentDeny:
                    response = HandleCommandAgentDeny(applicationMessage);
                    break;
                case ApplicationMessageType.CommandRunDeployment:
                    response = HandleCommandRunDeployment(applicationMessage);
                    break;
            }
            return _serializer.Serialize<ApplicationMessage>(response);
        }

        private ApplicationMessage HandleAgentFileInfo(ApplicationMessage message)
        {
            var agentFileInfo = _serializer.Deserialize<AgentFileInfo>(message.Data);
            FileInfo fileInfo = null;
            string sha256 = null;
            try
            {
                fileInfo = _fileRepository.GetFileInfo(agentFileInfo.FileUri);
                sha256 = _fileRepository.GetFileSha256(agentFileInfo.FileUri);
            }
            catch (FileNotFoundException)
            {
                return GetMasterError("File not found");
            }

            var masterFileInfo = new MasterFileInfo
            {
                Size = fileInfo.Length,
                IsDirectory = (fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory,
                Sha256 = sha256
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
            byte[] bytes;
            try
            {
                bytes = _fileRepository.GetFileChunk(agentFileChunk.FileUri, agentFileChunk.Offset, agentFileChunk.Size);
            }
            catch (FileNotFoundException)
            {
                return GetMasterError("File not found");
            }

            var masterFileChunk = new MasterFileChunk
            {
                Data = bytes
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

        private ApplicationMessage HandleCommandAgentList(ApplicationMessage message)
        {
            var commandAgentDeny = _serializer.Deserialize<CommandAgentList>(message.Data);
            return null;
        }

        private ApplicationMessage HandleCommandAgentAccept(ApplicationMessage message)
        {
            var commandAgentDeny = _serializer.Deserialize<CommandAgentAccept>(message.Data);
            _clientAuthInfoCache.Accept("fingerprint here");
            return null;
        }

        private ApplicationMessage HandleCommandAgentDeny(ApplicationMessage message)
        {
            var commandAgentDeny = _serializer.Deserialize<CommandAgentDeny>(message.Data);
            _clientAuthInfoCache.Deny("fingerprint here");
            _publisher.Aes.RegenerateKey();
            _sessionCache.Clear();
            return null;
        }

        private ApplicationMessage HandleCommandRunDeployment(ApplicationMessage message)
        {
            var commandAgentDeny = _serializer.Deserialize<CommandAgentDeny>(message.Data);
            _clientAuthInfoCache.Deny("fingerprint here");
            _publisher.Aes.RegenerateKey();
            _sessionCache.Clear();
            return null;
        }

        private ApplicationMessage GetMasterError(string errorMessage)
        {
            var masterError = new MasterError
            {
                ErrorMessage = errorMessage
            };
            var masterErrorMessage = new ApplicationMessage
            {
                Type = ApplicationMessageType.MasterError,
                Data = _serializer.Serialize<MasterError>(masterError)
            };
            return masterErrorMessage;
        }
    }
}