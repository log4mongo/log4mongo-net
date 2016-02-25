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
		private IMongoCollection<BsonDocument> _collection;
		private IMongoDatabase _db;
		private const string LogsCollectionName = "logs";

		[SetUp]
		public void SetUp()
		{
			GlobalContext.Properties.Clear();
			ThreadContext.Properties.Clear();
			MongoUrl url = new MongoUrl("mongodb://localhost/log4net");

			MongoClient client = new MongoClient(url);
			_db = client.GetDatabase(url.DatabaseName);
			_db.DropCollectionAsync(LogsCollectionName);
			_collection = _db.GetCollection<BsonDocument>(LogsCollectionName);
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
		public async void Should_log_timestamp()
		{
			var target = GetConfiguredLog();

			target.Info("a log");

			var doc = await _collection.FindAsync<BsonDocument>(new BsonDocument());
			await doc.ToListAsync().ContinueWith(l =>
			{
				var log = l.Result.FirstOrDefault();
				log.GetElement("timestamp").Value.Should().Be.OfType<BsonDateTime>();
				AssertTimestampLogged(log);
			});
		}

		[Test]
		public async void Should_log_level()
		{
			var target = GetConfiguredLog();

			target.Info("a log");

			var doc = await _collection.FindAsync<BsonDocument>(new BsonDocument());
			await doc.ToListAsync().ContinueWith(l =>
			{
				var log = l.Result.FirstOrDefault();
				log.GetElement("level").Value.Should().Be.OfType<BsonString>();
				log.GetElement("level").Value.AsString.Should().Be.EqualTo("INFO");
			});

		}

		[Test]
		public async void Should_log_thread()
		{
			var target = GetConfiguredLog();
			var threadName = Thread.CurrentThread.Name;

			target.Info("a log");

			var doc = await _collection.FindAsync<BsonDocument>(new BsonDocument());
			await doc.ToListAsync().ContinueWith(l =>
			{
				var log = l.Result.FirstOrDefault();
				log.GetElement("thread").Value.Should().Be.OfType<BsonString>();
				log.GetElement("thread").Value.AsString.Should().Be.EqualTo(threadName);
			});
		}

		[Test]
		public async void Should_log_exception()
		{
			var target = GetConfiguredLog();

			try
			{
				throw new ApplicationException("BOOM");
			}
			catch (Exception e)
			{
				target.Fatal("a log", e);
			}

			var doc = await _collection.FindAsync<BsonDocument>(new BsonDocument());
			await doc.ToListAsync().ContinueWith(l =>
			{
				var log = l.Result.FirstOrDefault();
				log.GetElement("exception").Value.Should().Be.OfType<BsonString>();
				log.GetElement("exception").Value.AsString.Should().Contain("ApplicationException: BOOM");
			});
		}

		[Test]
		public async void Should_log_threadcontext_properties()
		{
			var target = GetConfiguredLog();

			ThreadContext.Properties["threadContextProperty"] = "value";

			target.Info("a log");

			var doc = await _collection.FindAsync<BsonDocument>(new BsonDocument());
			await doc.ToListAsync().ContinueWith(l =>
			{
				var log = l.Result.FirstOrDefault();
				log.GetElement("threadContextProperty").Value.AsString.Should().Be.EqualTo("value");
			});

		}

		[Test]
		public async void Should_log_globalcontext_properties()
		{
			var target = GetConfiguredLog();

			GlobalContext.Properties["globalContextProperty"] = "value";

			target.Info("a log");
			var doc = await _collection.FindAsync<BsonDocument>(new BsonDocument());
			await doc.ToListAsync().ContinueWith(l =>
			{
				var log = l.Result.FirstOrDefault();
				log.GetElement("globalContextProperty").Value.AsString.Should().Be.EqualTo("value");
			});


		}

		[Test]
		public async void Should_preserve_type_of_properties()
		{
			var target = GetConfiguredLog();

			GlobalContext.Properties["numberProperty"] = 123;
			ThreadContext.Properties["dateProperty"] = DateTime.Now;

			target.Info("a log");

			var doc = await _collection.FindAsync<BsonDocument>(new BsonDocument());
			await doc.ToListAsync().ContinueWith(l =>
			{
				var log = l.Result.FirstOrDefault();
				log.GetElement("numberProperty").Value.Should().Be.OfType<BsonInt32>();
				log.GetElement("dateProperty").Value.Should().Be.OfType<BsonDateTime>();
			});
		}

		[Test]
		public async void Should_log_bsondocument()
		{
			ILog target = GetConfiguredLog();
			var customProperty = new
			{
				Start = DateTime.Now,
				Finished = DateTime.Now,
				Input = new
				{
					Count = 100
				},
				Output = new
				{
					Count = 95
				}
			};
			ThreadContext.Properties["customProperty"] = customProperty.ToBsonDocument();

			target.Info("Finished");

			var doc = await _collection.FindAsync<BsonDocument>(new BsonDocument());
			await doc.ToListAsync().ContinueWith(l =>
			{
				var log = l.Result.FirstOrDefault();
				string customPropertyFromDbJson = log["customProperty"].ToJson();
				customProperty.ToJson().Should().Be.EqualTo(customPropertyFromDbJson);
			});
		}

		[Test]
		public void Should_tolerate_null_raw_property()
		{
			ILog target = GetConfiguredLog();
			ThreadContext.Properties["customProperty"] = null;

			target.Info("Finished");

			_collection.CountAsync(new BsonDocument()).ContinueWith(c => c.Result.Should().Be.EqualTo(1));
		}

		[Test]
		public async void Should_log_standard_document_if_no_fields_defined()
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
			var threadName = Thread.CurrentThread.Name;

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

			var doc = await _collection.FindAsync<BsonDocument>(new BsonDocument());
			await doc.ToListAsync().ContinueWith(l =>
			{
				var log = l.Result.FirstOrDefault();

				AssertTimestampLogged(log);
				log.GetElement("level").Value.AsString.Should().Be.EqualTo("FATAL");
				log.GetElement("thread").Value.AsString.Should().Be.EqualTo(threadName);
				log.GetElement("userName").Value.AsString.Should().Be.EqualTo(WindowsIdentity.GetCurrent().Name);
				log.GetElement("message").Value.AsString.Should().Be.EqualTo("a log");
				log.GetElement("loggerName").Value.AsString.Should().Be.EqualTo("Test");
				log.GetElement("domain").Value.AsString.Should().Be.EqualTo(AppDomain.CurrentDomain.FriendlyName);
				log.GetElement("machineName").Value.AsString.Should().Be.EqualTo(Environment.MachineName);

				log.GetElement("fileName").Value.AsString.Should().EndWith("MongoDBAppenderTest.cs");
				//log.GetElement("method")
				//    .Value.AsString.Should()
				//    .Be.EqualTo("Should_log_standard_document_if_no_fields_defined");
				log.GetElement("lineNumber").Value.AsString.Should().Match(@"\d+");
				log.GetElement("className").Value.AsString.Should().StartWith("Log4Mongo.Tests.MongoDBAppenderTest");

				var exception = log.GetElement("exception").Value.AsBsonDocument;
				exception.GetElement("message").Value.AsString.Should().Be.EqualTo("BOOM");

				var properties = log.GetElement("properties").Value.AsBsonDocument;
				properties.GetElement("GlobalContextProperty").Value.AsString.Should().Be.EqualTo("GlobalContextValue");
				properties.GetElement("ThreadContextProperty").Value.AsString.Should().Be.EqualTo("ThreadContextValue");
			});
		}

		[Test]
		public async void Should_create_expiry_index()
		{
			XmlConfigurator.Configure(new MemoryStream(Encoding.UTF8.GetBytes(@"
		    <log4net>
			    <appender name='MongoDBAppender' type='Log4Mongo.MongoDBAppender, Log4Mongo'>
				    <connectionString value='mongodb://localhost' />
                    <expireAfterSeconds value='5' />
			    </appender>
			    <root>
				    <level value='ALL' />
				    <appender-ref ref='MongoDBAppender' />
			    </root>
		    </log4net>
		    ")));
			var target = LogManager.GetLogger("Test");

			target.Info("a log");

			using (var cursor = await _collection.Indexes.ListAsync())
			{
				var indexes = await cursor.ToListAsync();
				var expireAfterIndex = indexes.Single(p => p.ContainsValue(BsonValue.Create("expireAfterSecondsIndex")));
				expireAfterIndex.GetElement("expireAfterSeconds").Value.AsDouble.Should().Be.EqualTo(5);
			}
		}


		[Test]
		public async void Should_use_connection_from_connectionstrings_section_if_provided()
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

			var doc = await _collection.FindAsync<BsonDocument>(new BsonDocument());
			await doc.ToListAsync().ContinueWith(l =>
			{
				var log = l.Result.FirstOrDefault();
				log.GetElement("message").Value.AsString.Should().Be.EqualTo("a log");
			});
		}

		[Test]
		public async void Should_use_connection_from_connectionstring_if_provided_connectionstringname_is_wrong()
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

			var doc = await _collection.FindAsync<BsonDocument>(new BsonDocument());
			await doc.ToListAsync().ContinueWith(l =>
			{
				var log = l.Result.FirstOrDefault();
				log.GetElement("message").Value.AsString.Should().Be.EqualTo("a log");
			});

		}

		[Test]
		public async void Should_log_in_batch()
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

			await _collection.CountAsync(new BsonDocument()).ContinueWith(c => c.Result.Should().Be.EqualTo(0));

			target.Info(6);
			await _collection.FindAsync<BsonDocument>(new BsonDocument()).ContinueWith(c =>
				  {
					  c.Result.ToListAsync().ContinueWith(cc =>
					  {
						  cc.Result.Select(x => x.GetElement("message").Value.AsString)
							  .Should()
							  .Have.SameSequenceAs(new[] { "1", "2", "3", "4", "5", "6" });
					  });
				  });

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

			AssertThatCollectionIsNotCapped();
		}

		private void AssertThatCollectionIsNotCapped()
		{
			var stats = GetCollectionStats();
			stats.Elements.Should().Not.Contain(new BsonElement("capped", true));
		}

		private BsonDocument GetCollectionStats()
		{
			var command = new BsonDocumentCommand<BsonDocument>(new BsonDocument
			{
				{"collstats", LogsCollectionName}
			});

			return _db.RunCommandAsync(command).Result;
		}

		[Test]
		public void Should_create_capped_collection()
		{
			XmlConfigurator.Configure(new MemoryStream(Encoding.UTF8.GetBytes(@"
<log4net>
	<appender name='MongoDBAppender' type='Log4Mongo.MongoDBAppender, Log4Mongo'>
		<connectionString value='mongodb://localhost' />
		<newCollectionMaxSize value='65536' />
		<newCollectionMaxDocs value='5000' />
	</appender>
	<root>
		<level value='ALL' />
		<appender-ref ref='MongoDBAppender' />
	</root>
</log4net>
")));
			var target = LogManager.GetLogger("Test");

			target.Info("a log");

			AssertThatCollectionIsCapped(65536, 5000);
		}

		private void AssertThatCollectionIsCapped(int maxSize, int? maxDocs = null)
		{
			var stats = GetCollectionStats();
			stats.Elements.Should().Contain(new BsonElement("capped", true));
			stats.Elements.Should().Contain(new BsonElement("storageSize", maxSize));

			if (maxDocs.HasValue)
			{
				stats.Elements.Should().Contain(new BsonElement("max", maxDocs));
			}
		}

		[Test]
		public void Should_create_capped_collection_when_only_size_is_specified()
		{
			XmlConfigurator.Configure(new MemoryStream(Encoding.UTF8.GetBytes(@"
<log4net>
	<appender name='MongoDBAppender' type='Log4Mongo.MongoDBAppender, Log4Mongo'>
		<connectionString value='mongodb://localhost' />
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

			AssertThatCollectionIsCapped(65536);
		}

		[TestCase("4096", 4096, "1k", 1000)]
		[TestCase("10MB", 10485760, "1000", 1000)]
		[TestCase("5MB", 5242880, "3k", 3000)]
		public void Should_accept_units_in_collection_cap_values(string maxSizeString, int maxSize, string maxDocsString, int maxDocs)
		{
			XmlConfigurator.Configure(new MemoryStream(Encoding.UTF8.GetBytes(string.Format(@"
<log4net>
	<appender name='MongoDBAppender' type='Log4Mongo.MongoDBAppender, Log4Mongo'>
		<connectionString value='mongodb://localhost' />
		<newCollectionMaxSize value='{1}' />
		<newCollectionMaxDocs value='{0}' />
	</appender>
	<root>
		<level value='ALL' />
		<appender-ref ref='MongoDBAppender' />
	</root>
</log4net>
", maxDocsString, maxSizeString))));
			var target = LogManager.GetLogger("Test");

			target.Info("a log");

			AssertThatCollectionIsCapped(maxSize, maxDocs);
		}

		[Test]
		public void Should_not_cap_collection_when_units_invalid()
		{
			XmlConfigurator.Configure(new MemoryStream(Encoding.UTF8.GetBytes(@"
<log4net>
	<appender name='MongoDBAppender' type='Log4Mongo.MongoDBAppender, Log4Mongo'>
		<connectionString value='mongodb://localhost' />
		<newCollectionMaxSize value='12g' />
	</appender>
	<root>
		<level value='ALL' />
		<appender-ref ref='MongoDBAppender' />
	</root>
</log4net>
")));
			var target = LogManager.GetLogger("Test");

			target.Info("a log");

			AssertThatCollectionIsNotCapped();
		}

        [Test]
        public void Should_connect_over_ssl_connection_using_certificate_friendly_name()
        {
            XmlConfigurator.Configure(new MemoryStream(Encoding.UTF8.GetBytes(@"
<log4net>
	<appender name='MongoDBAppender' type='Log4Mongo.MongoDBAppender, Log4Mongo'>
        <connectionString value='mongodb://username:password@10.1.1.12:27018/databasename?ssl=true;sslVerifyCertificate=false'/>
        <certificateFriendlyName value='certificateFriendlyName' />
    </appender>
	<root>
		<level value='ALL' />
		<appender-ref ref='MongoDBAppender' />
	</root>
</log4net>
")));
            var target = LogManager.GetLogger("Test");

            target.Info("a log");
        }
    }
}
