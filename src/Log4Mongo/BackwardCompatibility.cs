﻿using System;
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
            if (!string.IsNullOrEmpty(appender.UserName) && !string.IsNullOrEmpty(appender.Password))
            {
                // use MongoDB authentication
                mongoConnectionString.AppendFormat(";Username={0};Password={1}", appender.UserName, appender.Password);
            }

            MongoServer connection = MongoServer.Create(mongoConnectionString.ToString()); // TODO Should be replaced with MongoClient, but this will change default for WriteConcern. See http://blog.mongodb.org/post/36666163412/introducing-mongoclient and http://docs.mongodb.org/manual/release-notes/drivers-write-concern
            connection.Connect();
            return connection.GetDatabase(appender.DatabaseName ?? "log4net_mongodb");
        }

        public static BsonDocument BuildBsonDocument(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
            {
                return null;
            }

            var toReturn = new BsonDocument {
				{"timestamp", loggingEvent.TimeStamp}, 
				{"level", loggingEvent.Level.ToString()}, 
				{"thread", loggingEvent.ThreadName}, 
				{"userName", loggingEvent.UserName}, 
				{"message", loggingEvent.RenderedMessage}, 
				{"loggerName", loggingEvent.LoggerName}, 
				{"domain", loggingEvent.Domain}, 
				{"machineName", Environment.MachineName}
			};

            // location information, if available
            if (loggingEvent.LocationInformation != null)
            {
                toReturn.Add("fileName", loggingEvent.LocationInformation.FileName);
                toReturn.Add("method", loggingEvent.LocationInformation.MethodName);
                toReturn.Add("lineNumber", loggingEvent.LocationInformation.LineNumber);
                toReturn.Add("className", loggingEvent.LocationInformation.ClassName);
            }

            // exception information
            if (loggingEvent.ExceptionObject != null)
            {
                toReturn.Add("exception", BuildExceptionBsonDocument(loggingEvent.ExceptionObject));
            }

            // properties
            PropertiesDictionary compositeProperties = loggingEvent.GetProperties();
            if (compositeProperties == null || compositeProperties.Count <= 0)
                return toReturn;

            var properties = new BsonDocument();
            foreach (DictionaryEntry entry in compositeProperties)
            {
                BsonValue value;
                properties.Add(entry.Key.ToString().Replace("log4net:", ""),
                    !BsonTypeMapper.TryMapToBsonValue(entry.Value, out value) ? entry.Value.ToBsonDocument() : value);
            }

            toReturn.Add("properties", properties);

            return toReturn;
        }

        private static BsonDocument BuildExceptionBsonDocument(Exception ex)
        {
            var toReturn = new BsonDocument {
				{"message", ex.Message}, 
				{"source", ex.Source}, 
				{"stackTrace", ex.StackTrace}
			};

            if (ex.InnerException != null)
            {
                toReturn.Add("innerException", BuildExceptionBsonDocument(ex.InnerException));
            }

            return toReturn;
        }
    }
}