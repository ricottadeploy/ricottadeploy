using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ricotta.Cryptography;
using Ricotta.Serialization;
using Ricotta.Transport;
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

        private ApplicationMessage HandleAgentFileInfo(ApplicationMessage applicationMessage)
        {
            var agentFileInfo = _serializer.Deserialize<AgentFileInfo>(applicationMessage.Data);
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
            var masterFileInfoBytes = _serializer.Serialize<MasterFileInfo>(masterFileInfo);
            var responseApplicationMessage = new ApplicationMessage
            {
                Type = ApplicationMessageType.MasterFileInfo,
                Data = masterFileInfoBytes
            };
            return responseApplicationMessage;
        }

        private ApplicationMessage HandleAgentFileChunk(ApplicationMessage applicationMessage)
        {
            var agentFileChunk = _serializer.Deserialize<AgentFileChunk>(applicationMessage.Data);
            byte[] chunk;
            try
            {
                chunk = _fileRepository.GetFileChunk(agentFileChunk.FileUri, agentFileChunk.Offset, agentFileChunk.Size);
            }
            catch (FileNotFoundException)
            {
                return GetMasterError("File not found");
            }

            var masterFileChunk = new MasterFileChunk
            {
                Data = chunk
            };
            var masterFileChunkBytes = _serializer.Serialize<MasterFileChunk>(masterFileChunk);
            var responseApplicationMessage = new ApplicationMessage
            {
                Type = ApplicationMessageType.MasterFileChunk,
                Data = masterFileChunkBytes
            };
            return responseApplicationMessage;
        }

        private ApplicationMessage HandleAgentModuleInfo(ApplicationMessage applicationMessage)
        {
            var agentModuleInfo = _serializer.Deserialize<AgentModuleInfo>(applicationMessage.Data);
            var masterModuleInfo = new MasterModuleInfo
            {
                FileUri = null
            };
            var masterModuleInfoBytes = _serializer.Serialize<MasterModuleInfo>(masterModuleInfo);
            var responseApplicationMessage = new ApplicationMessage
            {
                Type = ApplicationMessageType.MasterModuleInfo,
                Data = masterModuleInfoBytes
            };
            return responseApplicationMessage;
        }

        private ApplicationMessage HandleCommandAgentList(ApplicationMessage applicationMessage)
        {
            var commandAgentList = _serializer.Deserialize<CommandAgentList>(applicationMessage.Data);
            var clientAuthInfoList = _clientAuthInfoCache.GetList().Where(x => x.ClientId != "!").ToList();
            var masterAgentList = new MasterAgentList
            {
                Agents = clientAuthInfoList
            };
            var masterAgentListBytes = _serializer.Serialize<MasterAgentList>(masterAgentList);
            var responseApplicationMessage = new ApplicationMessage
            {
                Type = ApplicationMessageType.MasterAgentList,
                Data = masterAgentListBytes
            };
            return responseApplicationMessage;
        }

        private ApplicationMessage HandleCommandAgentAccept(ApplicationMessage applicationMessage)
        {
            var commandAgentAccept = _serializer.Deserialize<CommandAgentAccept>(applicationMessage.Data);
            var acceptedIds = new List<string>();
            _clientAuthInfoCache.AcceptById(commandAgentAccept.Selector);
            acceptedIds.Add(commandAgentAccept.Selector);
            var masterAgentAccept = new MasterAgentAccept
            {
                Agents = acceptedIds
            };
            var masterAgentAcceptBytes = _serializer.Serialize<MasterAgentAccept>(masterAgentAccept);
            var responseApplicationMessage = new ApplicationMessage
            {
                Type = ApplicationMessageType.MasterAgentAccept,
                Data = masterAgentAcceptBytes
            };
            return responseApplicationMessage;
        }

        private ApplicationMessage HandleCommandAgentDeny(ApplicationMessage applicationMessage)
        {
            var commandAgentDeny  = _serializer.Deserialize<CommandAgentDeny>(applicationMessage.Data);
            var deniedIds = new List<string>();
            _clientAuthInfoCache.DenyById(commandAgentDeny.Selector);
            deniedIds.Add(commandAgentDeny.Selector);
            var masterAgentDeny = new MasterAgentDeny
            {
                Agents = deniedIds
            };
            var masterAgentDenyBytes = _serializer.Serialize<MasterAgentDeny>(masterAgentDeny);
            var responseApplicationMessage = new ApplicationMessage
            {
                Type = ApplicationMessageType.MasterAgentDeny,
                Data = masterAgentDenyBytes
            };
            return responseApplicationMessage;
        }

        private ApplicationMessage HandleCommandRunDeployment(ApplicationMessage applicationMessage)
        {
            var commandRunDeployment = _serializer.Deserialize<CommandRunDeployment>(applicationMessage.Data);
            Log.Debug($"Run Deployment: {commandRunDeployment.DeploymentYaml}");
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