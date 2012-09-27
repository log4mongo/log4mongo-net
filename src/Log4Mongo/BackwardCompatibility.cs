using System;
using System.Collections;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using log4net.Core;
using log4net.Util;

namespace Log4Mongo
{
	public class BackwardCompatibility
	{
		public static MongoDatabase GetDatabase(MongoDBAppender appender)
		{
			var port = appender.Port > 0 ? appender.Port : 27017;
			var mongoConnectionString = new StringBuilder(string.Format("Server={0}:{1}", appender.Host ?? "localhost", port));
			if(!string.IsNullOrEmpty(appender.UserName) && !string.IsNullOrEmpty(appender.Password))
			{
				// use MongoDB authentication
				mongoConnectionString.AppendFormat(";Username={0};Password={1}", appender.UserName, appender.Password);
			}

			MongoServer connection = MongoServer.Create(mongoConnectionString.ToString());
			connection.Connect();
			return connection.GetDatabase(appender.DatabaseName ?? "log4net_mongodb");
		}

		public static BsonDocument BuildBsonDocument(LoggingEvent loggingEvent)
		{
			if(loggingEvent == null)
			{
				return null;
			}

			var toReturn = new BsonDocument();
			toReturn["timestamp"] = loggingEvent.TimeStamp;
			toReturn["level"] = loggingEvent.Level.ToString();
			toReturn["thread"] = loggingEvent.ThreadName;
			toReturn["userName"] = loggingEvent.UserName;
			toReturn["message"] = loggingEvent.RenderedMessage;
			toReturn["loggerName"] = loggingEvent.LoggerName;
			toReturn["domain"] = loggingEvent.Domain;
			toReturn["machineName"] = Environment.MachineName;

			// location information, if available
			if(loggingEvent.LocationInformation != null)
			{
				toReturn["fileName"] = loggingEvent.LocationInformation.FileName;
				toReturn["method"] = loggingEvent.LocationInformation.MethodName;
				toReturn["lineNumber"] = loggingEvent.LocationInformation.LineNumber;
				toReturn["className"] = loggingEvent.LocationInformation.ClassName;
			}

			// exception information
			if(loggingEvent.ExceptionObject != null)
			{
				toReturn["exception"] = BuildExceptionBsonDocument(loggingEvent.ExceptionObject);
			}

			// properties
			PropertiesDictionary compositeProperties = loggingEvent.GetProperties();
			if(compositeProperties != null && compositeProperties.Count > 0)
			{
				var properties = new BsonDocument();
				foreach(DictionaryEntry entry in compositeProperties)
				{
					properties[entry.Key.ToString()] = entry.Value.ToString();
				}

				toReturn["properties"] = properties;
			}

			return toReturn;
		}

		private static BsonDocument BuildExceptionBsonDocument(Exception ex)
		{
			var toReturn = new BsonDocument();
			toReturn["message"] = ex.Message;
			toReturn["source"] = ex.Source ?? string.Empty;
			toReturn["stackTrace"] = ex.StackTrace ?? string.Empty;

			if(ex.InnerException != null)
			{
				toReturn["innerException"] = BuildExceptionBsonDocument(ex.InnerException);
			}

			return toReturn;
		}
	}
}