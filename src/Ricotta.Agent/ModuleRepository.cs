using Ricotta.Transport;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ricotta.Agent
{
    public class ModuleRepository
    {
        public ModuleRepository(string repositoryPath, Client client)
        {
            throw new NotImplementedException();
        }

        public string GetModuleLocalPath(string moduleName)
        {
            throw new NotImplementedException();
        }

        public bool ExistsLocally(string moduleName)
        {
            throw new NotImplementedException();
        }

        public bool ExistsOnServer(string moduleName)
        {
            throw new NotImplementedException();
        }

        public bool Download(string moduleName)
        {
            throw new NotImplementedException();
        }
    }
}
