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
        private Client _client;

        public FileRepository(string repositoryPath, ISerializer serializer, Client client)
        {
            _respositoryPath = repositoryPath;
            _serializer = serializer;
            _client = client;
        }

        public bool Download(string fileUri)
        {
            var agentFileInfo = new AgentFileInfo
            {
                FileUri = fileUri
            };
            var agentFileInfoBytes = _serializer.Serialize<AgentFileInfo>(agentFileInfo);
            var applicationMessage = new ApplicationMessage
            {
                Type = ApplicationMessageType.AgentFileInfo,
                Data = agentFileInfoBytes
            };
            var applicationMessageBytes = _serializer.Serialize<ApplicationMessage>(applicationMessage);
            _client.SendApplicationData(applicationMessageBytes);

            var receivedApplicationMessageBytes = _client.ReceiveApplicationData();
            var receivedApplicationMessage = _serializer.Deserialize<ApplicationMessage>(receivedApplicationMessageBytes);
            if (receivedApplicationMessage.Type == ApplicationMessageType.MasterError)
            {
                var masterError = _serializer.Deserialize<MasterError>(receivedApplicationMessage.Data);
                Log.Error($"Error while downloading {fileUri}: {masterError.ErrorMessage}");
                return false;
            }
            else if (receivedApplicationMessage.Type == ApplicationMessageType.MasterFileInfo)
            {
                var masterFileInfo = _serializer.Deserialize<MasterFileInfo>(receivedApplicationMessage.Data);
                return Download(fileUri, masterFileInfo);
            }
            return false;
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
            var agentFileChunk = new AgentFileChunk
            {
                FileUri = fileUri,
                Offset = offset,
                Size = size
            };
            var agentFileChunkBytes = _serializer.Serialize<AgentFileChunk>(agentFileChunk);
            var applicationMessage = new ApplicationMessage
            {
                Type = ApplicationMessageType.AgentFileChunk,
                Data = agentFileChunkBytes
            };
            var applicationMessageBytes = _serializer.Serialize<ApplicationMessage>(applicationMessage);
            _client.SendApplicationData(applicationMessageBytes);

            var receivedApplicationMessageBytes = _client.ReceiveApplicationData();
            var receivedApplicationMessage = _serializer.Deserialize<ApplicationMessage>(receivedApplicationMessageBytes);
            if (receivedApplicationMessage.Type == ApplicationMessageType.MasterError)
            {
                var masterError = _serializer.Deserialize<MasterError>(receivedApplicationMessage.Data);
                Log.Error($"Error while downloading {fileUri}: {masterError.ErrorMessage}");
                return false;
            }
            else if (receivedApplicationMessage.Type == ApplicationMessageType.MasterFileChunk)
            {
                var masterFileChunk = _serializer.Deserialize<MasterFileChunk>(receivedApplicationMessage.Data);
                file.Write(masterFileChunk.Data, 0, masterFileChunk.Data.Length);
                return true;
            }
            return false;
        }
    }
}