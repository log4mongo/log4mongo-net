using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using log4net.Appender;
using log4net.Core;

namespace Log4Mongo
{
	public class MongoDBAppender : AppenderSkeleton
	{
		private readonly List<MongoAppenderFileld> _fields = new List<MongoAppenderFileld>();

		/// <summary>
		/// MongoDB database connection in the format:
		/// mongodb://[username:password@]host1[:port1][,host2[:port2],...[,hostN[:portN]]][/[database][?options]]
		/// See http://www.mongodb.org/display/DOCS/Connections
		/// If no database specified, default to "log4net"
		/// </summary>
		public string ConnectionString { get; set; }

		/// <summary>
		/// The connectionString name to use in the connectionStrings section of your *.config file
		/// If not specified or connectionString name does not exist will use ConnectionString value
		/// </summary>
		public string ConnectionStringName { get; set; }

		/// <summary>
		/// Name of the collection in database
		/// Defaults to "logs"
		/// </summary>
		public string CollectionName { get; set; }

		/// <summary>
		/// If set, create a TTL index to expire after specified number of seconds
		/// </summary>
		public long ExpireAfterSeconds { get; set; }

		/// <summary>
		/// Maximum number of documents in collection
		/// See http://docs.mongodb.org/manual/core/capped-collections/
		/// </summary>
		public string NewCollectionMaxDocs { get; set; }

		/// <summary>
		/// Maximum size of collection
		/// See http://docs.mongodb.org/manual/core/capped-collections/
		/// </summary>
		public string NewCollectionMaxSize { get; set; }

		#region Deprecated

		/// <summary>
		/// Hostname of MongoDB server
		/// Defaults to localhost
		/// </summary>
		[Obsolete("Use ConnectionString")]
		public string Host { get; set; }

		/// <summary>
		/// Port of MongoDB server
		/// Defaults to 27017
		/// </summary>
		[Obsolete("Use ConnectionString")]
		public int Port { get; set; }

		/// <summary>
		/// Name of the database on MongoDB
		/// Defaults to log4net_mongodb
		/// </summary>
		[Obsolete("Use ConnectionString")]
		public string DatabaseName { get; set; }

		/// <summary>
		/// MongoDB database user name
		/// </summary>
		[Obsolete("Use ConnectionString")]
		public string UserName { get; set; }

		/// <summary>
		/// MongoDB database password
		/// </summary>
		[Obsolete("Use ConnectionString")]
		public string Password { get; set; }

		#endregion

		public void AddField(MongoAppenderFileld fileld)
		{
			_fields.Add(fileld);
		}

		protected override void Append(LoggingEvent loggingEvent)
		{
			var collection = GetCollection();
			collection.InsertOneAsync(BuildBsonDocument(loggingEvent));
			CreateExpiryAfterIndex(collection);
		}

		protected override void Append(LoggingEvent[] loggingEvents)
		{
			var collection = GetCollection();
			collection.InsertManyAsync(loggingEvents.Select(BuildBsonDocument));
			CreateExpiryAfterIndex(collection);
		}

		private IMongoCollection<BsonDocument> GetCollection()
		{
			var db = GetDatabase();
			var collectionName = CollectionName ?? "logs";

			EnsureCollectionExists(db, collectionName);

			var collection = db.GetCollection<BsonDocument>(collectionName);
			return collection;
		}

		private void EnsureCollectionExists(IMongoDatabase db, string collectionName)
		{
			if (!CollectionExists(db, collectionName))
			{
				CreateCollection(db, collectionName);
			}
		}

		private bool CollectionExists(IMongoDatabase db, string collectionName)
		{
			var filter = new BsonDocument("name", collectionName);

			return db.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter })
					 .Result
					 .ToListAsync()
					 .Result
					 .Any();
		}

		private void CreateCollection(IMongoDatabase db, string collectionName)
		{
			var cob = new CreateCollectionOptions();

			SetCappedCollectionOptions(cob);

			db.CreateCollectionAsync(collectionName, cob).GetAwaiter().GetResult();
		}

		private void SetCappedCollectionOptions(CreateCollectionOptions options)
		{
			var unitResolver = new UnitResolver();

			var newCollectionMaxSize = unitResolver.Resolve(NewCollectionMaxSize);
			var newCollectionMaxDocs = unitResolver.Resolve(NewCollectionMaxDocs);

			if (newCollectionMaxSize > 0)
			{
				options.Capped = true;
				options.MaxSize = newCollectionMaxSize;

				if (newCollectionMaxDocs > 0)
				{
					options.MaxDocuments = newCollectionMaxDocs;
				}
			}
		}

		private string GetConnectionString()
		{
			ConnectionStringSettings connectionStringSetting = ConfigurationManager.ConnectionStrings[ConnectionStringName];
			return connectionStringSetting != null ? connectionStringSetting.ConnectionString : ConnectionString;
		}

		private IMongoDatabase GetDatabase()
		{
			string connStr = GetConnectionString();

			if (string.IsNullOrWhiteSpace(connStr))
			{
				throw new InvalidOperationException("Must provide a valid connection string");
			}

			MongoUrl url = MongoUrl.Create(connStr);
			MongoClient client = new MongoClient(url);
			IMongoDatabase db = client.GetDatabase(url.DatabaseName ?? "log4net");
			return db;
		}

		private BsonDocument BuildBsonDocument(LoggingEvent log)
		{
			if (_fields.Count == 0)
			{
				return BackwardCompatibility.BuildBsonDocument(log);
			}
			var doc = new BsonDocument();
			foreach (MongoAppenderFileld field in _fields)
			{
				object value = field.Layout.Format(log);
				var bsonValue = value as BsonValue ?? BsonValue.Create(value);
				doc.Add(field.Name, bsonValue);
			}
			return doc;
		}

		private void CreateExpiryAfterIndex(IMongoCollection<BsonDocument> collection)
		{
			if (ExpireAfterSeconds <= 0) return;
			collection.Indexes.CreateOneAsync(
				Builders<BsonDocument>.IndexKeys.Ascending("timestamp"),
				new CreateIndexOptions()
				{
					Name = "expireAfterSecondsIndex",
					ExpireAfter = new TimeSpan(ExpireAfterSeconds * TimeSpan.TicksPerSecond)
				});
		}
	}
}