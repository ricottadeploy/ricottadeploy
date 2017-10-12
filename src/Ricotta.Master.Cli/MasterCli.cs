using Microsoft.Extensions.Configuration;
using Ricotta.Cryptography;
using Ricotta.Serialization;
using Ricotta.Transport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Ricotta.Transport.Messages.Application;

namespace Ricotta.Master.Cli
{
    public class MasterCli : IMasterCli
    {
        private ISerializer _serializer;
        private IConfigurationRoot _config;

        public MasterCli(ISerializer serializer, IConfigurationRoot config)
        {
            _serializer = serializer;
            _config = config;
        }

        public void Start(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: TODO");
                Environment.Exit(0);
            }

            var group = args[0].ToLower();

            switch (group)
            {
                case "agent":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: TODO");
                        Environment.Exit(1);
                    }
                    else
                    {
                        var command = args[1].ToLower();
                        switch (command)
                        {
                            case "list":
                                if (args.Length >= 3)
                                {
                                    AgentList(args[2]);
                                }
                                else
                                {
                                    AgentList();
                                }
                                break;
                            case "accept":
                                if (args.Length < 3)
                                {
                                    Console.WriteLine("Usage: TODO");
                                    Environment.Exit(1);
                                }
                                var selectorAccept = args[2].ToLower();
                                AgentAccept(selectorAccept);
                                break;
                            case "deny":
                                if (args.Length < 3)
                                {
                                    Console.WriteLine("Usage: TODO");
                                    Environment.Exit(1);
                                }
                                var selectorDeny = args[2].ToLower();
                                AgentDeny(selectorDeny);
                                break;
                            default:
                                Console.WriteLine("Usage: TODO");
                                Environment.Exit(1);
                                break;
                        }
                    }
                    break;
                case "run":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: TODO");
                        Environment.Exit(1);
                    }
                    var yamlFile = args[1];
                    var yamlContent = File.ReadAllText(yamlFile);
                    RunDeployment(yamlContent);
                    break;
                default:
                    Console.WriteLine("Usage: TODO");
                    break;
            }
        }

        private Rsa GetRsa()
        {
            var rsaPrivateKeyPath = @"C:\ricottadev\master\keys\master\private.pem";
            var rsaPrivateKeyPem = File.ReadAllText(rsaPrivateKeyPath);
            var rsa = Rsa.CreateFromPrivatePEM(rsaPrivateKeyPem);
            return rsa;
        }

        private string GetRequestUrl()
        {
            var requestUrl = _config["bind:request_url"];
            var port = requestUrl.Split(":").Last();
            var url = $"tcp://127.0.0.1:{port}";
            return url;
        }

        private void AgentList(string filter = null)
        {
            var rsa = GetRsa();
            var client = new Client("!", _serializer, GetRsa(), GetRequestUrl());
            var result = client.TryAuthenticating();
            if (result != ClientStatus.Accepted)
            {
                Console.WriteLine("Error while authenticating with master");
                Environment.Exit(0);
            }
            var commandAgentList = new CommandAgentList
            {
                Filter = filter
            };
            var applicationMessage = new ApplicationMessage
            {
                Type = ApplicationMessageType.CommandAgentList,
                Data = _serializer.Serialize<CommandAgentList>(commandAgentList)
            };
            var bytes = _serializer.Serialize<ApplicationMessage>(applicationMessage);
            client.SendApplicationData(bytes);
            var response = client.ReceiveApplicationData();
            var responseMessage = _serializer.Deserialize<ApplicationMessage>(response);
            if (responseMessage.Type == ApplicationMessageType.MasterAgentList)
            {
                var masterAgentList = _serializer.Deserialize<MasterAgentList>(responseMessage.Data);
                foreach (var agent in masterAgentList.Agents)
                {
                    Console.WriteLine($"{agent.RsaFingerprint} {agent.ClientId}\t{agent.AuthenticationStatus}");
                }
            }
            else
            {
                Console.WriteLine("Error");
            }
        }

        private void AgentAccept(string selector)
        {
        }

        private void AgentDeny(string selector)
        {
        }

        private void RunDeployment(string yamlContent)
        {
        }
    }
}
