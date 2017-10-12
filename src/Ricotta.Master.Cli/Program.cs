using System;
using System.IO;
using Ricotta.Cryptography;
using Ricotta.Serialization;
using Ricotta.Transport;
using Ricotta.Transport.Messages.Application;

namespace Ricotta.Master.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var serializer = new MsgPackSerializer();
            var requestUrl = "tcp://127.0.0.1:5557";
            var privatePem = File.ReadAllText(@"C:\ricottadev\master\keys\master\private.pem");
            var rsa = Rsa.CreateFromPrivatePEM(privatePem);
            var client = new Client("!", serializer, rsa, requestUrl);
            var result = client.TryAuthenticating(2000);
            if (result == ClientStatus.Accepted)
            {
                var commandRunDeployment = new CommandRunDeployment
                {
                    DeploymentYaml = "test.yaml"
                };
                var applicationMessage = new ApplicationMessage
                {
                    Type = ApplicationMessageType.CommandRunDeployment,
                    Data = serializer.Serialize<CommandRunDeployment>(commandRunDeployment)
                };
                var bytes = serializer.Serialize<ApplicationMessage>(applicationMessage);
                client.SendApplicationData(bytes);
            }
        }
    }
}
