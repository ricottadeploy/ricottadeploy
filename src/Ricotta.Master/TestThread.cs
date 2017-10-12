using System.Threading;
using Serilog;
using Ricotta.Cryptography;
using Ricotta.Transport;

namespace Ricotta.Master
{
    public class TestThread
    {
        private Publisher _publisher;

        public TestThread(Publisher publisher)
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