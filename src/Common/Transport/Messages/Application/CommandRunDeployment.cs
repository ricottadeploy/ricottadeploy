using System;
using MessagePack;

namespace Ricotta.Transport.Messages.Application
{
    [MessagePackObject]
    public class CommandRunDeployment
    {
        [Key(0)]
        public string DeploymentYaml;
    }
}