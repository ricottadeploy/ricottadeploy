using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ricotta.Cryptography;
using Ricotta.Serialization;
using Ricotta.Transport;
using Ricotta.Transport.Messages.Application;
using Serilog;
using NuGet.Versioning;
using Common.Transport;

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
        private AppServer _appServer;

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
            _appServer = new AppServer(_serializer, _rsa, _publisher, _sessionCache, _clientAuthInfoCache, _workersUrl);
            _appServer.OnAgentFileInfoReceived(HandleAgentFileInfo);
            _appServer.OnAgentFileChunkReceived(HandleAgentFileChunk);
            _appServer.OnAgentModuleInfoReceived(HandleAgentModuleInfo);
            _appServer.OnAgentJobLogReceived(HandleAgentJobLog);
            _appServer.OnAgentJobResultReceived(HandleAgentJobResult);
            _appServer.OnCommandAgentListReceived(HandleCommandAgentList);
            _appServer.OnCommandAgentAcceptReceived(HandleCommandAgentAccept);
            _appServer.OnCommandAgentDenyReceived(HandleCommandAgentDeny);
            _appServer.OnCommandRunDeploymentReceived(HandleCommandRunDeployment);
            _appServer.Listen();
        }

        private ApplicationMessage HandleAgentFileInfo(AgentFileInfo agentFileInfo)
        {
            FileInfo fileInfo = null;
            string sha256 = null;
            try
            {
                fileInfo = _fileRepository.GetFileInfo(agentFileInfo.FileUri);
                sha256 = _fileRepository.GetFileSha256(agentFileInfo.FileUri);
            }
            catch (FileNotFoundException)
            {
                return _appServer.GetMasterError("File not found");
            }
            var isDirectory = (fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
            return _appServer.GetMasterFileInfo(fileInfo.Length, isDirectory, sha256);
        }

        private ApplicationMessage HandleAgentFileChunk(AgentFileChunk agentFileChunk)
        {
            byte[] chunk;
            try
            {
                chunk = _fileRepository.GetFileChunk(agentFileChunk.FileUri, agentFileChunk.Offset, agentFileChunk.Size);
            }
            catch (FileNotFoundException)
            {
                return _appServer.GetMasterError("File not found");
            }
            return _appServer.GetMasterFileChunk(chunk);
        }

        private ApplicationMessage HandleAgentModuleInfo(AgentModuleInfo agentModuleInfo)
        {
            var moduleRepositoryPath = Path.Combine(_fileRepository.Path, "modules");
            var modulePath = Path.Combine(moduleRepositoryPath, agentModuleInfo.ModuleName);
            var latestVersionPackagePath = NuGetPackageVersion.GetLatestVersionPackagePath(modulePath);
            if (latestVersionPackagePath == null)
            {
                return _appServer.GetMasterError($"Module does not exist");
            }
            var fileUri = latestVersionPackagePath.Replace(_fileRepository.Path, "").Substring(1);
            return _appServer.GetMasterModuleInfo(fileUri);
        }

        private ApplicationMessage HandleAgentJobLog(AgentJobLog agentJobLog)
        {
            Log.Information(agentJobLog.Message);
            var masterJobLog = new MasterJobLog { };
            var masterJobLogBytes = _serializer.Serialize<MasterJobLog>(masterJobLog);
            return _appServer.GetMasterJobLog();
        }

        private ApplicationMessage HandleAgentJobResult(AgentJobResult agentJobResult)
        {
            var result = "Success";
            if (agentJobResult.ErrorCode != 0)
            {
                result = "Failed";
            }
            Log.Information($"{agentJobResult.AgentId} : Job ID {agentJobResult.JobId} : {result}");
            if (agentJobResult.ErrorCode != 0)
            {
                Log.Error($"{agentJobResult.AgentId} : Job ID {agentJobResult.JobId} : Error : {agentJobResult.ErrorMessage}");
            }
            if (agentJobResult.ResultData != null)
            {
                Log.Information($"{agentJobResult.AgentId} : Job ID {agentJobResult.JobId} : Result : {agentJobResult.ResultData}");
            }
            return _appServer.GetMasterJobResult();
        }

        private ApplicationMessage HandleCommandAgentList(CommandAgentList commandAgentList)
        {
            var clientAuthInfoList = _clientAuthInfoCache.GetList().Where(x => x.ClientId != "!").ToList();
            return _appServer.GetMasterAgentList(clientAuthInfoList);
        }

        private ApplicationMessage HandleCommandAgentAccept(CommandAgentAccept commandAgentAccept)
        {
            var acceptedIds = new List<string>();
            _clientAuthInfoCache.AcceptById(commandAgentAccept.Selector);
            acceptedIds.Add(commandAgentAccept.Selector);
            return _appServer.GetMasterAgentAccept(acceptedIds);
        }

        private ApplicationMessage HandleCommandAgentDeny(CommandAgentDeny commandAgentDeny)
        {
            var deniedIds = new List<string>();
            _clientAuthInfoCache.DenyById(commandAgentDeny.Selector);
            deniedIds.Add(commandAgentDeny.Selector);
            return _appServer.GetMasterAgentDeny(deniedIds);
        }

        private ApplicationMessage HandleCommandRunDeployment(CommandRunDeployment commandRunDeployment)
        {
            Log.Debug($"Run Deployment: {commandRunDeployment.DeploymentYaml}");
            return _appServer.GetMasterRunDeployment();
        }

    }
}