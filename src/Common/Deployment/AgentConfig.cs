using System;
using System.Collections.Generic;
using System.Text;

namespace Ricotta.Deployment
{
    public class AgentConfig
    {
        public string id { get; set; }
        public string fingerprint { get; set; }
        public List<string> roles { get; set; }
        public bool deny { get; set; }
    }
}
