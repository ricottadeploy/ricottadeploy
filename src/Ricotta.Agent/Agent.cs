using System;
using Microsoft.Extensions.Configuration;
using Ricotta.Serialization;
using Serilog;

namespace Ricotta.Agent
{
    public class Agent : IAgent
    {
        public Agent(IConfigurationRoot config, ISerializer serializer)
        {
            Log.Information($"Ricotta Agent");
        }
        public void Start()
        {
        }

    }
}