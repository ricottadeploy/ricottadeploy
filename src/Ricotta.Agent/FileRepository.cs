using Common.Cryptography;
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
    public class FileRepository
    {
        private string _respositoryPath;
        private ISerializer _serializer;
        private AppClient _appClient;

        public FileRepository(string repositoryPath, ISerializer serializer, AppClient appClient)
        {
            _respositoryPath = repositoryPath;
            _serializer = serializer;
            _appClient = appClient;
        }

        public bool Download(string fileUri)
        {
            _appClient.SendAgentFileInfo(fileUri);
            try
            {
                var masterFileInfo = _appClient.ReceiveMasterFileInfo();
                return Download(fileUri, masterFileInfo);
            }
            catch (Exception e)
            {
                Log.Error($"Error while downloading {fileUri}: {e.Message}");
                return false;
            }
        }

        private bool Download(string fileUri, MasterFileInfo masterFileInfo)
        {
            var localFilePath = Path.Combine(_respositoryPath, fileUri);
            var localFileDirectory = Path.GetDirectoryName(localFilePath);
            var tempFileName = $".download.{DateTime.Now.ToString("yyyyMMddHHmmss")}";
            var tempFilePath = Path.Combine(localFileDirectory, tempFileName);
            Directory.CreateDirectory(localFileDirectory);
            using (var tempFile = File.Create(tempFilePath))
            {
                var chunkSize = 1000000;
                var chunkCount = (int)Math.Ceiling(masterFileInfo.Size / (double)chunkSize);
                if (masterFileInfo.Size > 0 && chunkCount == 0)
                {
                    chunkCount = 1;
                }
                for (int i = 0; i < chunkCount; i++)
                {
                    var offset = i * chunkSize;
                    int size = chunkSize;
                    if (masterFileInfo.Size - offset < chunkSize)
                    {
                        size = (int)masterFileInfo.Size - offset;
                    }
                    var success = DownloadFileChunk(fileUri, offset, size, tempFile);
                    if (!success)
                    {
                        return false;
                    }
                }
            }

            var localHash = Sha256.CalculateFileHash(tempFilePath);
            if (localHash != masterFileInfo.Sha256)
            {
                Log.Error("Download failed - SHA256 mismatch");
                return false;
            }

            if (File.Exists(localFilePath))
            {
                File.Delete(localFilePath);
            }
            File.Move(tempFilePath, localFilePath);
            return true;
        }

        private bool DownloadFileChunk(string fileUri, int offset, int size, FileStream file)
        {
            _appClient.SendAgentFileChunk(fileUri, offset, size);
            try
            {
                var masterFileChunk = _appClient.ReceiveMasterFileChunk();

                file.Write(masterFileChunk.Data, 0, masterFileChunk.Data.Length);
                return true;
            }
            catch (Exception e)
            {
                Log.Error($"Error while downloading {fileUri}: {e.Message}");
                return false;
            }
        }
    }
}