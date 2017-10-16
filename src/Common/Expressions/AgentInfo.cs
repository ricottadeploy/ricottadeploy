using System;
using System.Collections.Generic;
using System.Text;

namespace Ricotta.Common.Expressions
{
    public class AgentInfo
    {
        private string _environment;
        private string _id;
        private List<string> _roles;

        public string Environment
        {
            get
            {
                return _environment;
            }
        }

        public string Id
        {
            get
            {
                return _id;
            }
        }

        public List<string> Roles
        {
            get
            {
                return _roles;
            }
        }

        public AgentInfo(string environment, string id, List<string> roles)
        {
            _environment = environment;
            _id = id;
            _roles = roles;
        }

        public bool EvaluateSelector(string environment, string idOrRole)
        {
            var result = environment == _environment;
            return result && (idOrRole == _id || _roles.Contains(idOrRole));
        }
    }
}
