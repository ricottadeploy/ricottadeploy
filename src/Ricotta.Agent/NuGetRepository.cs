using NuGet.Versioning;
using Ricotta.Serialization;
using Ricotta.Transport;
using Ricotta.Transport.Messages.Application;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ricotta.Agent
{
    public class NuGetRepository
    {
        private string _respositoryPath;
        private ISerializer _serializer;
        private Client _client;
        private FileRepository _fileRepository;

        public NuGetRepository(string repositoryPath, ISerializer serializer, Client client, FileRepository fileRepository)
        {
            _respositoryPath = repositoryPath;
            _serializer = serializer;
            _client = client;
            _fileRepository = fileRepository;
        }

        public string GetPackagePath(string packageName)
        {
            var modulePath = Path.Combine(_respositoryPath, packageName);
            if (!Directory.Exists(modulePath))
            {
                return null;
            }
            return NuGetPackageVersion.GetLatestVersionPackagePath(modulePath);
        }

        public bool ExistsLocally(string packageName)
        {
            return GetPackagePath(packageName) != null;
        }

        public bool Download(string packageName)
        {
            var agentModuleInfo = new AgentModuleInfo
            {
                ModuleName = packageName
            };
            var agentModuleInfoBytes = _serializer.Serialize<AgentModuleInfo>(agentModuleInfo);
            var applicationMessage = new ApplicationMessage
            {
                Type = ApplicationMessageType.AgentModuleInfo,
                Data = agentModuleInfoBytes
            };
            var applicationMessageBytes = _serializer.Serialize<ApplicationMessage>(applicationMessage);
            _client.SendApplicationData(applicationMessageBytes);

            var receivedApplicationMessageBytes = _client.ReceiveApplicationData();
            var receivedApplicationMessage = _serializer.Deserialize<ApplicationMessage>(receivedApplicationMessageBytes);
            if (receivedApplicationMessage.Type == ApplicationMessageType.MasterError)
            {
                var masterError = _serializer.Deserialize<MasterError>(receivedApplicationMessage.Data);
                Log.Error($"Error while downloading module {packageName}: {masterError.ErrorMessage}");
                return false;
            }
            else if (receivedApplicationMessage.Type == ApplicationMessageType.MasterModuleInfo)
            {
                var masterModuleInfo = _serializer.Deserialize<MasterModuleInfo>(receivedApplicationMessage.Data);
                var success = _fileRepository.Download(masterModuleInfo.FileUri);
                return success;
            }
            return false;
        }
    }
}
