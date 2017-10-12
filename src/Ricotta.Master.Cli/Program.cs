using System;
using System.IO;
using Ricotta.Cryptography;
using Ricotta.Serialization;
using Ricotta.Transport;
using Ricotta.Transport.Messages.Application;
using Microsoft.Extensions.Configuration;
using Ricotta.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Ricotta.Master.Cli
{
    class Program
    {
        private static string _configFile = @"C:\ricottadev\master\config.yml";
        private static IConfigurationRoot Config { get; set; }

        static void Main(string[] args)
        {
            LoadConfiguration();
            ConfigureLogging();

            var services = new ServiceCollection();
            ConfigureServices(services);
            Start(services.BuildServiceProvider(), args);

            //var requestUrl = "tcp://127.0.0.1:5557";
            //var privatePem = File.ReadAllText(@"C:\ricottadev\master\keys\master\private.pem");
            //var rsa = Rsa.CreateFromPrivatePEM(privatePem);
            //var client = new Client("!", serializer, rsa, requestUrl);
            //var result = client.TryAuthenticating(2000);
            //if (result == ClientStatus.Accepted)
            //{
            //    var commandRunDeployment = new CommandRunDeployment
            //    {
            //        DeploymentYaml = "test.yaml"
            //    };
            //    var applicationMessage = new ApplicationMessage
            //    {
            //        Type = ApplicationMessageType.CommandRunDeployment,
            //        Data = serializer.Serialize<CommandRunDeployment>(commandRunDeployment)
            //    };
            //    var bytes = serializer.Serialize<ApplicationMessage>(applicationMessage);
            //    client.SendApplicationData(bytes);
            //}
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
                .AddSingleton<IMasterCli, MasterCli>();
        }

        private static void Start(IServiceProvider serviceProvider, string[] args)
        {
            var masterCli = serviceProvider.GetService<IMasterCli>();
            masterCli.Start(args);
        }
    }
}
