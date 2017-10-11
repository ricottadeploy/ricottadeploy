using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Ricotta.Cryptography;
using Ricotta.Serialization;
using Ricotta.Transport;
using Ricotta.Transport.Messages;
using Ricotta.Transport.Messages.Application;
using Serilog;

namespace Ricotta.Master
{
    public class TestThread
    {
        private Publisher _publisher;

        public TestThread(
                        Publisher publisher)
        {
            _publisher = publisher;
            while (true)
            {
                Log.Debug("Publising Test.Ping");
                _publisher.SendExecuteModuleMethod("*", Aes.Create().IV, "Test", "Ping", new object[] { "arg1", 2 });
                Thread.Sleep(10000);
            }
        }
    }
}