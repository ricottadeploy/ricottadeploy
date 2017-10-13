using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Serilog.Events;
using Ricotta.Transport;
using Ricotta.Transport.Messages.Application;
using Ricotta.Serialization;

namespace Serilog.Sinks.RicottaMaster
{
    internal class RicottaMasterSink : ILogEventSink
    {
        private Client _client;
        private string _agentId;
        private string _jobId;
        private ISerializer _serializer;

        public RicottaMasterSink(ISerializer serializer, Client client, string agentId, string jobId)
        {
            _serializer = serializer;
            _client = client;
            _agentId = agentId;
            _jobId = jobId;
        }

        public void Emit(LogEvent logEvent)
        {
            var message = $"[{_agentId}, {_jobId}] [{logEvent.Timestamp}] [{logEvent.Level}] {logEvent.MessageTemplate}";
            var agentJobLog = new AgentJobLog
            {
                AgentId = _agentId,
                JobId = _jobId,
                Message = message
            };
            var agentJobLogBytes = _serializer.Serialize<AgentJobLog>(agentJobLog);
            var applicationMessage = new ApplicationMessage
            {
                Type = ApplicationMessageType.AgentJobLog,
                Data = agentJobLogBytes
            };
            var applicationMessageBytes = _serializer.Serialize<ApplicationMessage>(applicationMessage);
            _client.SendApplicationData(applicationMessageBytes);
            _client.ReceiveApplicationData();   // Ignore received message
        }
    }
}