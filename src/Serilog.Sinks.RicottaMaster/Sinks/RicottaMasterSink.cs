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
        private AppClient _appClient;
        private string _agentId;
        private string _jobId;
        private ISerializer _serializer;

        public RicottaMasterSink(ISerializer serializer, AppClient appClient, string agentId, string jobId)
        {
            _serializer = serializer;
            _appClient = appClient;
            _agentId = agentId;
            _jobId = jobId;
        }

        public void Emit(LogEvent logEvent)
        {
            var message = $"[{_agentId}, {_jobId}] [{logEvent.Timestamp}] [{logEvent.Level}] {logEvent.MessageTemplate}";
            _appClient.SendAgentJobLog(_agentId, _jobId, message);
            _appClient.ReceiveMasterJobLog();   // Ignore received message
        }
    }
}