using System;
using log4net;

namespace log4net_MongoDB.Tests
{
    public class Program
    {
        private static ILog log = LogManager.GetLogger(typeof(Program));

        public static void Main(string[] args)
        {
            // initialize log4net logging
            log4net.Config.XmlConfigurator.Configure();

            log.Info("Hello from log4mongo !");
            
            Console.ReadLine();
        }
    }
}
