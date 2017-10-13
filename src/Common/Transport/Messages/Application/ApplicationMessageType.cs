using System;

namespace Ricotta.Transport.Messages.Application
{
    public enum ApplicationMessageType
    {
        // Agent to master req server
        AgentFileInfo = 0, MasterFileInfo,
        AgentFileChunk, MasterFileChunk,
        AgentModuleInfo, MasterModuleInfo,
        AgentJobLog, MasterJobLog,
        AgentJobResult, MasterJobResult,
        // CLI to master req server
        CommandAgentList, MasterAgentList,
        CommandAgentAccept, MasterAgentAccept,
        CommandAgentDeny, MasterAgentDeny,
        CommandRunDeployment, MasterRunDeployment,
        MasterError
    }
}