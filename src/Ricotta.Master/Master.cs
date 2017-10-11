using System;
using Microsoft.Extensions.Configuration;
using Ricotta.Serialization;
using Serilog;

namespace Ricotta.Master
{
    public class Master : IMaster
    {
        public Master(IConfigurationRoot config, ISerializer serializer)
        {
            Log.Information($"Ricotta Master");
        }
        public void Start()
        {
        }

    }
}