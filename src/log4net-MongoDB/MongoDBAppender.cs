#region Licence

/*
 *   Copyright (c) 2010, Jozef Sevcik <sevcik@styxys.com>
 *   All rights reserved.
 *
 *   Redistribution and use in source and binary forms, with or without
 *   modification, are permitted provided that the following conditions are met:
 *   * Redistributions of source code must retain the above copyright
 *     notice, this list of conditions and the following disclaimer.
 *   * Redistributions in binary form must reproduce the above copyright
 *     notice, this list of conditions and the following disclaimer in the
 *     documentation and/or other materials provided with the distribution.
 *   * Neither the name of the <organization> nor the
 *     names of its contributors may be used to endorse or promote products
 *     derived from this software without specific prior written permission.
 *
 *   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 *   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 *   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 *   DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
 *   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 *   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 *   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 *   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 *   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 *   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
#endregion

using System;
using MongoDB.Driver;
using log4net.Core;

namespace log4net.Appender
{
    /// <summary>
    /// log4net Appender into MongoDB database
    /// </summary>
    public class MongoDBAppender : AppenderSkeleton
    {
        public const string DEFAULT_MONGO_HOST = "localhost";
        public const int DEFAULT_MONGO_PORT = 27017;
        public const string DEFAULT_DB_NAME = "log4net_mongodb";
        public const string DEFAULT_COLLECTION_NAME = "logs";

        private string hostname = DEFAULT_MONGO_HOST;
        private int port = DEFAULT_MONGO_PORT;
        private string dbName = DEFAULT_DB_NAME;
        private string collectionName = DEFAULT_COLLECTION_NAME;

        protected Mongo connection;
        protected IMongoCollection collection;

        protected override bool RequiresLayout
        {
            get { return false; }
        }

        #region Appender configuration properties

        /// <summary>
        /// Hostname of MongoDB server
        /// Defaults to DEFAULT_MONGO_HOST
        /// </summary>
        public string Host
        {
            get { return hostname; }
            set { hostname = value; }
        }

        /// <summary>
        /// Port of MongoDB server
        /// Defaults to DEFAULT_MONGO_PORT
        /// </summary>
        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        /// <summary>
        /// Name of the database on MongoDB
        /// Defaults to DEFAULT_DB_NAME
        /// </summary>
        public string DatabaseName
        {
            get { return dbName; }
            set { dbName = value; }
        }

        /// <summary>
        /// Name of the collection in database
        /// Defaults to DEFAULT_COLLECTION_NAME
        /// </summary>
        public string CollectionName
        {
            get { return collectionName; }
            set { collectionName = value; }
        }

        #endregion

        public override void ActivateOptions()
        {
            try
            {
                connection = new Mongo(Host, Port);
                connection.Connect();
                // TODO: support for authentication
                collection = connection.getDB(DatabaseName).GetCollection(CollectionName);
            }
            catch (Exception e)
            {
                ErrorHandler.Error("Exception while initializing MongoDB Appender", e);
            }
        }

        protected override void OnClose()
        {
            collection = null;
            connection.Disconnect();
            connection.Dispose();
            base.OnClose();
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            var doc = LoggingEventToBSON(loggingEvent);
            if (doc != null)
            {
                collection.Insert(doc);
            }
        }

        /// <summary>
        /// Create BSON representation of LoggingEvent
        /// </summary>
        /// <param name="loggingEvent"></param>
        /// <returns></returns>
        protected Document LoggingEventToBSON(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null) return null;

            var toReturn = new Document();
            toReturn["timestamp"] = loggingEvent.TimeStamp;
            toReturn["level"] = loggingEvent.Level.ToString();
            toReturn["thread"] = loggingEvent.ThreadName;
            toReturn["userName"] = loggingEvent.UserName;
            toReturn["message"] = loggingEvent.RenderedMessage;
                        
            // location information, if available
            if (loggingEvent.LocationInformation != null)
            {
                toReturn["fileName"] = loggingEvent.LocationInformation.FileName;
                toReturn["method"] = loggingEvent.LocationInformation.MethodName;
                toReturn["lineNumber"] = loggingEvent.LocationInformation.LineNumber;
                toReturn["className"] = loggingEvent.LocationInformation.ClassName;
            }

            // TODO: exception information        
            
            return toReturn;
        }
    }
}
