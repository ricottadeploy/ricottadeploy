using Ricotta.Cryptography;
using Ricotta.Serialization;
using Ricotta.Transport;
using Ricotta.Transport.Messages.Application;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ricotta.Transport
{
    public class AppClient
    {
        private ISerializer _serializer;
        private Client _client;

        public AppClient(ISerializer serializer,
                            Rsa rsa,
                            string clientId,
                            string serverUri)
        {
            _serializer = serializer;
            _client = new Client(clientId, serializer, rsa, serverUri);
        }

        public ClientStatus TryAuthenticating(int timeout = 0)
        {
            return _client.TryAuthenticating(timeout);
        }

        public byte[] GetMasterPublishKey()
        {
            return _client.Session.PublishKey;
        }

        public void SendAgentFileInfo(string fileUri)
        {
            var agentFileInfo = new AgentFileInfo
            {
                FileUri = fileUri
            };
            SendApplicationMessage<AgentFileInfo>(ApplicationMessageType.AgentFileInfo, agentFileInfo);
        }

        public MasterFileInfo ReceiveMasterFileInfo()
        {
            var masterFileInfo = ReceiveApplicationMessage<MasterFileInfo>(ApplicationMessageType.MasterFileInfo);
            return masterFileInfo;
        }

        public void SendAgentFileChunk(string fileUri, int offset, int size)
        {
            var agentFileChunk = new AgentFileChunk
            {
                FileUri = fileUri,
                Offset = offset,
                Size = size
            };
            SendApplicationMessage<AgentFileChunk>(ApplicationMessageType.AgentFileChunk, agentFileChunk);
        }

        public MasterFileChunk ReceiveMasterFileChunk()
        {
            var masterFileChunk = ReceiveApplicationMessage<MasterFileChunk>(ApplicationMessageType.MasterFileChunk);
            return masterFileChunk;
        }

        public void SendAgentModuleInfo(string packageName)
        {
            var agentModuleInfo = new AgentModuleInfo
            {
                ModuleName = packageName
            };
            SendApplicationMessage<AgentModuleInfo>(ApplicationMessageType.AgentModuleInfo, agentModuleInfo);
        }

        public MasterModuleInfo ReceiveMasterModuleInfo()
        {
            var masterModuleInfo = ReceiveApplicationMessage<MasterModuleInfo>(ApplicationMessageType.MasterModuleInfo);
            return masterModuleInfo;
        }

        public void SendAgentJobLog(string agentId, string jobId, string message)
        {
            var agentJobLog = new AgentJobLog
            {
                AgentId = agentId,
                JobId = jobId,
                Message = message
            };
            SendApplicationMessage<AgentJobLog>(ApplicationMessageType.AgentJobLog, agentJobLog);
        }

        public MasterJobLog ReceiveMasterJobLog()
        {
            var masterJobLog = ReceiveApplicationMessage<MasterJobLog>(ApplicationMessageType.MasterJobLog);
            return masterJobLog;
        }

        public void SendAgentJobResult(string agentId, string jobId, int errorCode, string errorMessage, string resultData)
        {
            var agentJobResult = new AgentJobResult
            {
                AgentId = agentId,
                JobId = jobId,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                ResultData = resultData
            };
            SendApplicationMessage<AgentJobResult>(ApplicationMessageType.AgentJobResult, agentJobResult);
        }

        public MasterJobResult ReceiveMasterJobResult()
        {
            var masterJobResult = ReceiveApplicationMessage<MasterJobResult>(ApplicationMessageType.MasterJobResult);
            return masterJobResult;
        }

        private void SendApplicationMessage<T>(ApplicationMessageType type, T message)
        {
            var applicationMessage = new ApplicationMessage
            {
                Type = type,
                Data = _serializer.Serialize<T>(message)
            };
            var applicationMessageBytes = _serializer.Serialize<ApplicationMessage>(applicationMessage);
            _client.SendApplicationData(applicationMessageBytes);
        }

        private T ReceiveApplicationMessage<T>(ApplicationMessageType type)
        {
            var applicationMessageBytes = _client.ReceiveApplicationData();
            var applicationMessage = _serializer.Deserialize<ApplicationMessage>(applicationMessageBytes);
            ThrowIfError(applicationMessage);
            if (applicationMessage.Type != type)
            {
                throw new Exception($"Unexpected message received of type {type}");
            }
            var t = _serializer.Deserialize<T>(applicationMessage.Data);
            return t;
        }

        private void ThrowIfError(ApplicationMessage applicationMessage)
        {
            if (applicationMessage.Type == ApplicationMessageType.MasterError)
            {
                var masterError = _serializer.Deserialize<MasterError>(applicationMessage.Data);
                throw new Exception(masterError.ErrorMessage);
            }
        }
    }
}
