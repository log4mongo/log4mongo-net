using System;
using System.IO;
using System.Text;
using System.Threading;
using log4net;
using log4net.Config;
using log4net.Util;

namespace Log4Mongo.LoggingConsole
{
	public class Program
	{
		private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);
		private static int _count;

		public static void Main()
		{
			LogLog.InternalDebugging = true;

			XmlConfigurator.Configure(new MemoryStream(Encoding.UTF8.GetBytes(@"<?xml version='1.0' encoding='utf-8' ?>
                <configuration>
                    <configSections>
                        <section name='log4net' type='log4net.Config.Log4NetConfigurationSectionHandler, log4net' />
                    </configSections>
                    <log4net>
                        <appender name='ConsoleAppender' type='log4net.Appender.ConsoleAppender'>
                            <layout type='log4net.Layout.SimpleLayout' />
                        </appender>
                        <appender name='MongoDBAppender' type='Log4Mongo.MongoDBAppender, Log4Mongo'>
                            <connectionString value='mongodb://localhost' />
                        </appender>
                        <root>
                            <level value='ALL' />
                            <appender-ref ref='MongoDBAppender' />
                            <appender-ref ref='ConsoleAppender' />
                        </root>
                    </log4net>
                </configuration>
            ")));

			ILog log = LogManager.GetLogger(typeof(Program));
			log.Info("Starting");
			while (!Console.KeyAvailable)
			{
				log.Info(++_count);
				Thread.Sleep(Interval);
			}
		}
	}
}
