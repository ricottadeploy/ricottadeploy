using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Ricotta.Logging;
using Ricotta.Serialization;

namespace Ricotta.Master
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
                .AddSingleton<IMaster, Master>();
        }

        private static void Start(IServiceProvider serviceProvider)
        {
            var master = serviceProvider.GetService<IMaster>();
            master.Start();
        }
    }
}
