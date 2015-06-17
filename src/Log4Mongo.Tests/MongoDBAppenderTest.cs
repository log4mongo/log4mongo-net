using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using SharpTestsEx;
using log4net;
using log4net.Config;

namespace Log4Mongo.Tests
{
	[TestFixture]
	public class MongoDBAppenderTest
	{
		private MongoCollection _collection;

		[SetUp]
		public void SetUp()
		{
			GlobalContext.Properties.Clear();
			ThreadContext.Properties.Clear();

			MongoServer conn = MongoServer.Create("mongodb://localhost");
			MongoDatabase db = conn.GetDatabase("log4net");
			db.DropCollection("logs");
			_collection = db.GetCollection("logs");
		}

		private ILog GetConfiguredLog()
		{
			XmlConfigurator.Configure(new MemoryStream(Encoding.UTF8.GetBytes(@"
<log4net>
	<appender name='MongoDBAppender' type='Log4Mongo.MongoDBAppender, Log4Mongo'>
		<connectionString value='mongodb://localhost' />
		<field>
			<name value='timestamp' />
			<layout type='log4net.Layout.RawTimeStampLayout' />
		</field>
		<field>
			<name value='level' />
			<layout type='log4net.Layout.PatternLayout' value='%level' />
		</field>
		<field>
			<name value='thread' />
			<layout type='log4net.Layout.PatternLayout' value='%thread' />
		</field>
		<field>
			<name value='threadContextProperty' />
			<layout type='log4net.Layout.RawPropertyLayout'>
				<key value='threadContextProperty' />
			</layout>
		</field>
		<field>
			<name value='globalContextProperty' />
			<layout type='log4net.Layout.RawPropertyLayout'>
				<key value='globalContextProperty' />
			</layout>
		</field>
		<field>
			<name value='numberProperty' />
			<layout type='log4net.Layout.RawPropertyLayout'>
				<key value='numberProperty' />
			</layout>
		</field>
		<field>
			<name value='dateProperty' />
			<layout type='log4net.Layout.RawPropertyLayout'>
				<key value='dateProperty' />
			</layout>
		</field>
		<field>
			<name value='exception' />
			<layout type='log4net.Layout.ExceptionLayout' />
		</field>
		<field>
			<name value='customProperty' />
			<layout type='log4net.Layout.RawPropertyLayout'>
				<key value='customProperty' />
			</layout>
		</field>
	</appender>
	<root>
		<level value='ALL' />
		<appender-ref ref='MongoDBAppender' />
	</root>
</log4net>
")));
			return LogManager.GetLogger("Test");
		}

		[Test]
		public void Should_log_timestamp()
		{
			var target = GetConfiguredLog();

			target.Info("a log");

			var doc = _collection.FindOneAs<BsonDocument>();
			doc.GetElement("timestamp").Value.Should().Be.OfType<BsonDateTime>();
			AssertTimestampLogged(doc);
		}

		[Test]
		public void Should_log_level()
		{
			var target = GetConfiguredLog();

			target.Info("a log");

			var doc = _collection.FindOneAs<BsonDocument>();
			doc.GetElement("level").Value.Should().Be.OfType<BsonString>();
			doc.GetElement("level").Value.AsString.Should().Be.EqualTo("INFO");
		}

		[Test]
		public void Should_log_thread()
		{
			var target = GetConfiguredLog();

			target.Info("a log");

			var doc = _collection.FindOneAs<BsonDocument>();
			doc.GetElement("thread").Value.Should().Be.OfType<BsonString>();
			doc.GetElement("thread").Value.AsString.Should().Be.EqualTo(Thread.CurrentThread.Name);
		}

		[Test]
		public void Should_log_exception()
		{
			var target = GetConfiguredLog();

			try
			{
				throw new ApplicationException("BOOM");
			}
			catch(Exception e)
			{
				target.Fatal("a log", e);
			}

			var doc = _collection.FindOneAs<BsonDocument>();
			doc.GetElement("exception").Value.Should().Be.OfType<BsonString>();
			doc.GetElement("exception").Value.AsString.Should().Contain("ApplicationException: BOOM");
		}

		[Test]
		public void Should_log_threadcontext_properties()
		{
			var target = GetConfiguredLog();

			ThreadContext.Properties["threadContextProperty"] = "value";

			target.Info("a log");

			var doc = _collection.FindOneAs<BsonDocument>();
			doc.GetElement("threadContextProperty").Value.AsString.Should().Be.EqualTo("value");
		}

		[Test]
		public void Should_log_globalcontext_properties()
		{
			var target = GetConfiguredLog();

			GlobalContext.Properties["globalContextProperty"] = "value";

			target.Info("a log");

			var doc = _collection.FindOneAs<BsonDocument>();
			doc.GetElement("globalContextProperty").Value.AsString.Should().Be.EqualTo("value");
		}

		[Test]
		public void Should_preserve_type_of_properties()
		{
			var target = GetConfiguredLog();

			GlobalContext.Properties["numberProperty"] = 123;
			ThreadContext.Properties["dateProperty"] = DateTime.Now;

			target.Info("a log");
	
			var doc = _collection.FindOneAs<BsonDocument>();
			doc.GetElement("numberProperty").Value.Should().Be.OfType<BsonInt32>();
			doc.GetElement("dateProperty").Value.Should().Be.OfType<BsonDateTime>();
		}

		[Test]
		public void Should_log_bsondocument()
		{
			ILog target = GetConfiguredLog();
			var customProperty = new {
				Start = DateTime.Now,
				Finished = DateTime.Now,
				Input = new {
					Count = 100
				},
				Output = new {
					Count = 95
				}
			};
			ThreadContext.Properties["customProperty"] = customProperty.ToBsonDocument();

			target.Info("Finished");

			string customPropertyFromDbJson = _collection.FindOneAs<BsonDocument>()["customProperty"].ToJson();
			customProperty.ToJson().Should().Be.EqualTo(customPropertyFromDbJson);
		}

		[Test]
		public void Should_tolerate_null_raw_property()
		{
			ILog target = GetConfiguredLog();
			ThreadContext.Properties["customProperty"] = null;

			target.Info("Finished");

			_collection.Count().Should().Be.EqualTo(1);
		}

		[Test]
		public void Should_log_standard_document_if_no_fields_defined()
		{
			XmlConfigurator.Configure(new MemoryStream(Encoding.UTF8.GetBytes(@"
<log4net>
	<appender name='MongoDBAppender' type='Log4Mongo.MongoDBAppender, Log4Mongo'>
		<connectionString value='mongodb://localhost' />
	</appender>
	<root>
		<level value='ALL' />
		<appender-ref ref='MongoDBAppender' />
	</root>
</log4net>
")));
			var target = LogManager.GetLogger("Test");

			GlobalContext.Properties["GlobalContextProperty"] = "GlobalContextValue";
			ThreadContext.Properties["ThreadContextProperty"] = "ThreadContextValue";

			try
			{
				throw new ApplicationException("BOOM");
			}
			catch (Exception e)
			{
				target.Fatal("a log", e);
			}

			var doc = _collection.FindOneAs<BsonDocument>();

			AssertTimestampLogged(doc); 
			doc.GetElement("level").Value.AsString.Should().Be.EqualTo("FATAL");
			doc.GetElement("thread").Value.AsString.Should().Be.EqualTo(Thread.CurrentThread.Name);
			doc.GetElement("userName").Value.AsString.Should().Be.EqualTo(WindowsIdentity.GetCurrent().Name);
			doc.GetElement("message").Value.AsString.Should().Be.EqualTo("a log");
			doc.GetElement("loggerName").Value.AsString.Should().Be.EqualTo("Test");
			doc.GetElement("domain").Value.AsString.Should().Be.EqualTo(AppDomain.CurrentDomain.FriendlyName);
			doc.GetElement("machineName").Value.AsString.Should().Be.EqualTo(Environment.MachineName);

			doc.GetElement("fileName").Value.AsString.Should().EndWith("MongoDBAppenderTest.cs");
			doc.GetElement("method").Value.AsString.Should().Be.EqualTo("Should_log_standard_document_if_no_fields_defined");
			doc.GetElement("lineNumber").Value.AsString.Should().Match(@"\d+");
			doc.GetElement("className").Value.AsString.Should().Be.EqualTo("Log4Mongo.Tests.MongoDBAppenderTest");

			var exception = doc.GetElement("exception").Value.AsBsonDocument;
			exception.GetElement("message").Value.AsString.Should().Be.EqualTo("BOOM");

			var properties = doc.GetElement("properties").Value.AsBsonDocument;
			properties.GetElement("GlobalContextProperty").Value.AsString.Should().Be.EqualTo("GlobalContextValue");
			properties.GetElement("ThreadContextProperty").Value.AsString.Should().Be.EqualTo("ThreadContextValue");
		}

		[Test]
		public void Should_use_legacy_connection_configuration_when_no_connectionstring_defined()
		{
			XmlConfigurator.Configure(new MemoryStream(Encoding.UTF8.GetBytes(@"
<log4net>
	<appender name='MongoDBAppender' type='Log4Mongo.MongoDBAppender, Log4Mongo'>
		<host value='localhost' />
		<port value='27017' />
		<databaseName value='log4net' />
		<collectionName value='logs' />
	</appender>
	<root>
		<level value='ALL' />
		<appender-ref ref='MongoDBAppender' />
	</root>
</log4net>
")));
			ILog target = LogManager.GetLogger("Test");

			target.Info("a log");

			var doc = _collection.FindOneAs<BsonDocument>();
			doc.GetElement("message").Value.AsString.Should().Be.EqualTo("a log");
		}

		[Test]
		public void Should_use_connection_from_connectionstrings_section_if_provided()
		{
			XmlConfigurator.Configure(new MemoryStream(Encoding.UTF8.GetBytes(@"
<log4net>
	<appender name='MongoDBAppender' type='Log4Mongo.MongoDBAppender, Log4Mongo'>
		<connectionStringName value='mongodb-log4net' /> <!-- see App.config for value to use -->
	</appender>
	<root>
		<level value='ALL' />
		<appender-ref ref='MongoDBAppender' />
	</root>
</log4net>
")));
			ILog target = LogManager.GetLogger("Test");

			target.Info("a log");

			var doc = _collection.FindOneAs<BsonDocument>();
			doc.GetElement("message").Value.AsString.Should().Be.EqualTo("a log");
		}

		[Test]
		public void Should_use_connection_from_connectionstring_if_provided_connectionstringname_is_wrong()
		{
			XmlConfigurator.Configure(new MemoryStream(Encoding.UTF8.GetBytes(@"
<log4net>
	<appender name='MongoDBAppender' type='Log4Mongo.MongoDBAppender, Log4Mongo'>
		<connectionStringName value='miss-spelt-connection-string' /> <!-- see App.config for missing name -->
		<connectionString value='mongodb://localhost' />
	</appender>
	<root>
		<level value='ALL' />
		<appender-ref ref='MongoDBAppender' />
	</root>
</log4net>
")));
			ILog target = LogManager.GetLogger("Test");

			target.Info("a log");

			var doc = _collection.FindOneAs<BsonDocument>();
			doc.GetElement("message").Value.AsString.Should().Be.EqualTo("a log");
		}

		[Test]
		public void Should_log_in_batch()
		{
			XmlConfigurator.Configure(new MemoryStream(Encoding.UTF8.GetBytes(@"
<log4net>
	<appender name='BufferingForwardingAppender' type='log4net.Appender.BufferingForwardingAppender' >
		<bufferSize value='5'/>
		<appender-ref ref='MongoDBAppender' />
	</appender>
	<appender name='MongoDBAppender' type='Log4Mongo.MongoDBAppender, Log4Mongo'>
		<connectionString value='mongodb://localhost' />
	</appender>
	<root>
		<level value='ALL' />
		<appender-ref ref='BufferingForwardingAppender' />
	</root>
</log4net>
")));
			var target = LogManager.GetLogger("Test");

			target.Info(1);
			target.Info(2);
			target.Info(3);
			target.Info(4);
			target.Info(5);

			_collection.Count().Should().Be.EqualTo(0);

			target.Info(6);

			_collection.FindAllAs<BsonDocument>().Select(x => x.GetElement("message").Value.AsString).Should().Have.SameSequenceAs(new[] { "1", "2", "3", "4", "5", "6" });

		}

		private static void AssertTimestampLogged(BsonDocument doc)
		{
			var now = DateTime.UtcNow;
			var oneSecondAgo = now.AddSeconds(-1);
			doc.GetElement("timestamp").Value.AsDateTime.Should().Be.IncludedIn(oneSecondAgo, now);
		}

		[Test]
		public void Should_not_create_capped_collection()
		{
			var target = GetConfiguredLog();

			target.Info("a log");

			_collection.GetStats().IsCapped.Should().Be(false);
		}

		[Test]
		public void Should_create_capped_collection()
		{
			XmlConfigurator.Configure(new MemoryStream(Encoding.UTF8.GetBytes(@"
<log4net>
	<appender name='MongoDBAppender' type='Log4Mongo.MongoDBAppender, Log4Mongo'>
		<connectionString value='mongodb://localhost' />
		<newCollectionMaxDocs value='5000' />
		<newCollectionMaxSize value='65536' />
	</appender>
	<root>
		<level value='ALL' />
		<appender-ref ref='MongoDBAppender' />
	</root>
</log4net>
")));
			var target = LogManager.GetLogger("Test");

			target.Info("a log");

			var stats = _collection.GetStats();
			stats.IsCapped.Should().Be(true);
			stats.MaxDocuments.Should().Be(5000);
			stats.StorageSize.Should().Be(65536);
		}
	}
}
