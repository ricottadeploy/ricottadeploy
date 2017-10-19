using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;

namespace Ricotta.Deployment
{
    public class EnvironmentConfig
    {
        private Dictionary<string, List<AgentConfig>> _environments = new Dictionary<string, List<AgentConfig>>();

        public Dictionary<string, List<AgentConfig>> Environments
        {
            get
            {
                return _environments;
            }
        }

        public void ReadFromYamlFile(string fileName)
        {
            var yamlContent = File.ReadAllText(fileName);
            var deserializer = new DeserializerBuilder().Build();
            _environments = deserializer.Deserialize<Dictionary<string, List<AgentConfig>>>(yamlContent);
        }

        public List<AgentConfig> Get(string environment)
        {
            return _environments[environment];
        }
    }
}
