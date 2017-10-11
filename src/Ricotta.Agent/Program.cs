using System;
using MessagePack;
using Ricotta.Serialization;
using Ricotta.Transport;
using Ricotta.Transport.Messages.SecurityLayer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using NetEscapades.Configuration.Yaml;
using Ricotta.Logging;
using System.IO;

namespace Ricotta.Agent
{
    class Program
    {
        private static string _configFile = "config.yml";
        private static IConfigurationRoot Config { get; set; }

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                _configFile = args[0];
            }
            LoadConfiguration();
            ConfigureLogging();

            var services = new ServiceCollection();
            ConfigureServices(services);
            Start(services.BuildServiceProvider());
        }
        
        private static void LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddYamlFile(_configFile);
            Config = builder.Build();
        }

        private static void ConfigureLogging()
        {
            var loggingLevel = Config["logging:level"];
            Log.Logger = LoggerFactory.Create(loggingLevel);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSingleton<IConfigurationRoot>(Config)
                .AddSingleton<ISerializer, MsgPackSerializer>()
                .AddSingleton<IAgent, Agent>();
        }

        private static void Start(IServiceProvider serviceProvider)
        {
            var agent = serviceProvider.GetService<IAgent>();
            agent.Start();
        }
    }
}
