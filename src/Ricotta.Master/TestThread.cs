using System.Threading;
using Serilog;
using Ricotta.Cryptography;
using Ricotta.Transport;
using System;

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
                _publisher.SendExecuteModuleMethod("*", Aes.Create().IV, $"test-{DateTime.Now.ToString("yyyyMMddHHmmss")}", "Test", "Ping", new object[] { "hello from master" });
                Thread.Sleep(10000);
                _publisher.SendExecuteModuleMethod("*", Aes.Create().IV, $"test-{DateTime.Now.ToString("yyyyMMddHHmmss")}", "Test", "Ping", new object[] { 123 });
                Thread.Sleep(10000);
            }
        }
    }
}