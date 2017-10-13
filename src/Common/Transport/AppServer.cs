using Ricotta.Cryptography;
using Ricotta.Serialization;
using Ricotta.Transport;
using Ricotta.Transport.Messages.Application;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Transport
{
    public class AppServer
    {
        public delegate ApplicationMessage OnAgentFileInfoReceivedCallback(AgentFileInfo agentFileInfo);
        private OnAgentFileInfoReceivedCallback _onAgentFileInfoReceivedCallback;
        public delegate ApplicationMessage OnAgentFileChunkReceivedCallback(AgentFileChunk agentFileChunk);
        private OnAgentFileChunkReceivedCallback _onAgentFileChunkReceivedCallback;
        public delegate ApplicationMessage OnAgentModuleInfoReceivedCallback(AgentModuleInfo agentModuleInfo);
        private OnAgentModuleInfoReceivedCallback _onAgentModuleInfoReceivedCallback;
        public delegate ApplicationMessage OnAgentJobLogReceivedCallback(AgentJobLog agentJobLog);
        private OnAgentJobLogReceivedCallback _onAgentJobLogReceivedCallback;
        public delegate ApplicationMessage OnAgentJobResultReceivedCallback(AgentJobResult agentJobResult);
        private OnAgentJobResultReceivedCallback _onAgentJobResultReceivedCallback;
        public delegate ApplicationMessage OnCommandAgentListReceivedCallback(CommandAgentList commandAgentList);
        private OnCommandAgentListReceivedCallback _onCommandAgentListReceivedCallback;
        public delegate ApplicationMessage OnCommandAgentAcceptReceivedCallback(CommandAgentAccept commandAgentAccept);
        private OnCommandAgentAcceptReceivedCallback _onCommandAgentAcceptReceivedCallback;
        public delegate ApplicationMessage OnCommandAgentDenyReceivedCallback(CommandAgentDeny commandAgentDeny);
        private OnCommandAgentDenyReceivedCallback _onCommandAgentDenyReceivedCallback;
        public delegate ApplicationMessage OnCommandRunDeploymentReceivedCallback(CommandRunDeployment commandRunDeployment);
        private OnCommandRunDeploymentReceivedCallback _onCommandRunDeploymentReceivedCallback;

        private Server _server;
        private ISerializer _serializer;

        public AppServer(ISerializer serializer,
                        Rsa rsa,
                        Publisher publisher,
                        SessionCache sessionCache,
                        ClientAuthInfoCache clientAuthInfoCache,
                        string workersUrl)
        {
            _serializer = serializer;
            _server = new Server(_serializer, rsa, publisher, sessionCache, clientAuthInfoCache, workersUrl);
            _server.OnApplicationDataReceived(ProcessApplicationMessages);
        }

        public void Listen()
        {
            _server.Listen();
        }

        public void OnAgentFileInfoReceived(Func<AgentFileInfo, ApplicationMessage> callback)
        {
            _onAgentFileInfoReceivedCallback = new OnAgentFileInfoReceivedCallback(callback);
        }

        public void OnAgentFileChunkReceived(Func<AgentFileChunk, ApplicationMessage> callback)
        {
            _onAgentFileChunkReceivedCallback = new OnAgentFileChunkReceivedCallback(callback);
        }

        public void OnAgentModuleInfoReceived(Func<AgentModuleInfo, ApplicationMessage> callback)
        {
            _onAgentModuleInfoReceivedCallback = new OnAgentModuleInfoReceivedCallback(callback);
        }

        public void OnAgentJobLogReceived(Func<AgentJobLog, ApplicationMessage> callback)
        {
            _onAgentJobLogReceivedCallback = new OnAgentJobLogReceivedCallback(callback);
        }

        public void OnAgentJobResultReceived(Func<AgentJobResult, ApplicationMessage> callback)
        {
            _onAgentJobResultReceivedCallback = new OnAgentJobResultReceivedCallback(callback);
        }

        public void OnCommandAgentListReceived(Func<CommandAgentList, ApplicationMessage> callback)
        {
            _onCommandAgentListReceivedCallback = new OnCommandAgentListReceivedCallback(callback);
        }

        public void OnCommandAgentAcceptReceived(Func<CommandAgentAccept, ApplicationMessage> callback)
        {
            _onCommandAgentAcceptReceivedCallback = new OnCommandAgentAcceptReceivedCallback(callback);
        }

        public void OnCommandAgentDenyReceived(Func<CommandAgentDeny, ApplicationMessage> callback)
        {
            _onCommandAgentDenyReceivedCallback = new OnCommandAgentDenyReceivedCallback(callback);
        }

        public void OnCommandRunDeploymentReceived(Func<CommandRunDeployment, ApplicationMessage> callback)
        {
            _onCommandRunDeploymentReceivedCallback = new OnCommandRunDeploymentReceivedCallback(callback);
        }

        private byte[] ProcessApplicationMessages(byte[] message)
        {
            var applicationMessage = _serializer.Deserialize<ApplicationMessage>(message);
            Log.Debug($"Received application message of type {applicationMessage.Type}");
            ApplicationMessage responseApplicationMessage = null;
            switch (applicationMessage.Type)
            {
                case ApplicationMessageType.AgentFileInfo:
                    var agentFileInfo = _serializer.Deserialize<AgentFileInfo>(applicationMessage.Data);
                    responseApplicationMessage = _onAgentFileInfoReceivedCallback(agentFileInfo);
                    break;
                case ApplicationMessageType.AgentFileChunk:
                    var agentFileChunk = _serializer.Deserialize<AgentFileChunk>(applicationMessage.Data);
                    responseApplicationMessage = _onAgentFileChunkReceivedCallback(agentFileChunk);
                    break;
                case ApplicationMessageType.AgentModuleInfo:
                    var agentModuleInfo = _serializer.Deserialize<AgentModuleInfo>(applicationMessage.Data);
                    responseApplicationMessage = _onAgentModuleInfoReceivedCallback(agentModuleInfo);
                    break;
                case ApplicationMessageType.AgentJobLog:
                    var agentJobLog = _serializer.Deserialize<AgentJobLog>(applicationMessage.Data);
                    responseApplicationMessage = _onAgentJobLogReceivedCallback(agentJobLog);
                    break;
                case ApplicationMessageType.AgentJobResult:
                    var agentJobResult = _serializer.Deserialize<AgentJobResult>(applicationMessage.Data);
                    responseApplicationMessage = _onAgentJobResultReceivedCallback(agentJobResult);
                    break;
                case ApplicationMessageType.CommandAgentList:
                    var commandAgentList = _serializer.Deserialize<CommandAgentList>(applicationMessage.Data);
                    responseApplicationMessage = _onCommandAgentListReceivedCallback(commandAgentList);
                    break;
                case ApplicationMessageType.CommandAgentAccept:
                    var commandAgentAccept = _serializer.Deserialize<CommandAgentAccept>(applicationMessage.Data);
                    responseApplicationMessage = _onCommandAgentAcceptReceivedCallback(commandAgentAccept);
                    break;
                case ApplicationMessageType.CommandAgentDeny:
                    var commandAgentDeny = _serializer.Deserialize<CommandAgentDeny>(applicationMessage.Data);
                    responseApplicationMessage = _onCommandAgentDenyReceivedCallback(commandAgentDeny);
                    break;
                case ApplicationMessageType.CommandRunDeployment:
                    var commandRunDeployment = _serializer.Deserialize<CommandRunDeployment>(applicationMessage.Data);
                    responseApplicationMessage = _onCommandRunDeploymentReceivedCallback(commandRunDeployment);
                    break;
            }
            return _serializer.Serialize<ApplicationMessage>(responseApplicationMessage);
        }

        public ApplicationMessage GetMasterError(string errorMessage)
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

        public ApplicationMessage GetMasterFileInfo(long size, bool isDirectory, string sha256)
        {
            var masterFileInfo = new MasterFileInfo
            {
                Size = size,
                IsDirectory = isDirectory,
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

        public ApplicationMessage GetMasterFileChunk(byte[] data)
        {
            var masterFileChunk = new MasterFileChunk
            {
                Data = data
            };
            var masterFileChunkBytes = _serializer.Serialize<MasterFileChunk>(masterFileChunk);
            var responseApplicationMessage = new ApplicationMessage
            {
                Type = ApplicationMessageType.MasterFileChunk,
                Data = masterFileChunkBytes
            };
            return responseApplicationMessage;
        }

        public ApplicationMessage GetMasterModuleInfo(string fileUri)
        {
            var masterModuleInfo = new MasterModuleInfo
            {
                FileUri = fileUri
            };
            var masterModuleInfoBytes = _serializer.Serialize<MasterModuleInfo>(masterModuleInfo);
            var responseApplicationMessage = new ApplicationMessage
            {
                Type = ApplicationMessageType.MasterModuleInfo,
                Data = masterModuleInfoBytes
            };
            return responseApplicationMessage;
        }

        public ApplicationMessage GetMasterJobLog()
        {
            var masterJobLog = new MasterJobLog
            {
            };
            var masterJobLogBytes = _serializer.Serialize<MasterJobLog>(masterJobLog);
            var responseApplicationMessage = new ApplicationMessage
            {
                Type = ApplicationMessageType.MasterJobLog,
                Data = masterJobLogBytes
            };
            return responseApplicationMessage;
        }

        public ApplicationMessage GetMasterJobResult()
        {
            var masterJobResult = new MasterJobResult
            {
            };
            var masterJobResultBytes = _serializer.Serialize<MasterJobResult>(masterJobResult);
            var responseApplicationMessage = new ApplicationMessage
            {
                Type = ApplicationMessageType.MasterJobResult,
                Data = masterJobResultBytes
            };
            return responseApplicationMessage;
        }

        public ApplicationMessage GetMasterAgentList(List<ClientAuthInfo> clientAuthInfoList)
        {
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

        public ApplicationMessage GetMasterAgentAccept(List<string> agentIds)
        {
            var masterAgentAccept = new MasterAgentAccept
            {
                Agents = agentIds
            };
            var masterAgentAcceptBytes = _serializer.Serialize<MasterAgentAccept>(masterAgentAccept);
            var responseApplicationMessage = new ApplicationMessage
            {
                Type = ApplicationMessageType.MasterAgentAccept,
                Data = masterAgentAcceptBytes
            };
            return responseApplicationMessage;
        }

        public ApplicationMessage GetMasterAgentDeny(List<string> agentIds)
        {
            var masterAgentDeny = new MasterAgentDeny
            {
                Agents = agentIds
            };
            var masterAgentDenyBytes = _serializer.Serialize<MasterAgentDeny>(masterAgentDeny);
            var responseApplicationMessage = new ApplicationMessage
            {
                Type = ApplicationMessageType.MasterAgentDeny,
                Data = masterAgentDenyBytes
            };
            return responseApplicationMessage;
        }

        public ApplicationMessage GetMasterRunDeployment()
        {
            var masterRunDeployment = new MasterRunDeployment
            {
            };
            var masterRunDeploymentBytes = _serializer.Serialize<MasterRunDeployment>(masterRunDeployment);
            var responseApplicationMessage = new ApplicationMessage
            {
                Type = ApplicationMessageType.MasterRunDeployment,
                Data = masterRunDeploymentBytes
            };
            return responseApplicationMessage;
        }

    }
}
