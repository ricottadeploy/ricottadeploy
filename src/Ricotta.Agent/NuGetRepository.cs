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
        private AppClient _appClient;
        private FileRepository _fileRepository;

        public NuGetRepository(string repositoryPath, ISerializer serializer, AppClient appClient, FileRepository fileRepository)
        {
            _respositoryPath = repositoryPath;
            _serializer = serializer;
            _appClient = appClient;
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
            _appClient.SendAgentModuleInfo(packageName);
            try
            {
                var masterModuleInfo = _appClient.ReceiveMasterModuleInfo();
                var success = _fileRepository.Download(masterModuleInfo.FileUri);
                return success;
            }
            catch (Exception e)
            {
                Log.Error($"Error while downloading module {packageName}: {e.Message}");
                return false;
            }
        }
    }
}
