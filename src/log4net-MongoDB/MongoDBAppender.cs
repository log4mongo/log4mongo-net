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
using System.Security;
using MongoDB.Driver;
using log4net.Core;
using System.Text;
using System.Globalization;
using log4net.DateFormatter;

namespace log4net.Appender
{
    /// <summary>
    /// log4net Appender into MongoDB database
    /// This appender does not use layout option
    /// Format of log event (for exception):
    /// <code>
    /// { 
    ///     "timestamp" : "04/25/2010 02:16:21,257",
    ///     "level": "ERROR", 
    ///     "thread": "7", 
    ///     "userName": "jsk", 
    ///     "message": "I'm sorry", 
    ///     "fileName": "C:\jsk\work\opensource\log4net-MongoDB\src\log4net-MongoDB.Tests\MongoDBAppenderTests.cs", 
    ///     "method": "TestException", 
    ///     "lineNumber": "102", 
    ///     "className": "log4net_MongoDB.Tests.MongoDBAppenderTests", 
    ///     "exception": { 
    ///                     "message": "Something wrong happened", 
    ///                     "source": null, 
    ///                     "stackTrace": null, 
    ///                     "innerException": { 
    ///                                         "message": "I'm the inner", 
    ///                                         "source": null, 
    ///                                         "stackTrace": null 
    ///                                       } 
    ///                  } 
    /// }
    /// </code>
    /// </summary>
    public class MongoDBAppender : AppenderSkeleton
    {
        protected const string DEFAULT_MONGO_HOST = "localhost";
        protected const int DEFAULT_MONGO_PORT = 27017;
        protected const string DEFAULT_DB_NAME = "log4net_mongodb";
        protected const string DEFAULT_COLLECTION_NAME = "logs";

        private const string TIMESTAMP_FORMAT = "yyyy-MM-dd HH:mm:ss,fff";

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

        /// <summary>
        /// Mongo collection used for logs
        /// The main reason of exposing this is to have same log collection available for unit tests
        /// </summary>
        public IMongoCollection LogCollection
        {
            get { return collection; }
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

        /// <summary>
        /// MongoDB database user name
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// MongoDB database password
        /// </summary>
        public string Password { get; set; }

        #endregion

        public override void ActivateOptions()
        {
            try
            {
                var mongoConnectionString = new StringBuilder(string.Format("Server={0}:{1}", Host, Port));
                if (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password))
                {
                    // use MongoDB authentication
                    mongoConnectionString.AppendFormat(";Username={0};Password={1}", UserName, Password);
                }

                connection = new Mongo(mongoConnectionString.ToString());
                connection.Connect();
                var db = connection.GetDatabase(DatabaseName);
                collection = db.GetCollection(CollectionName);
            }
            catch (Exception e)
            {
                ErrorHandler.Error("Exception while initializing MongoDB Appender", e, ErrorCode.GenericFailure);
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
            if (collection != null)
            {
                var doc = LoggingEventToBSON(loggingEvent);
                if (doc != null)
                {
                    collection.Insert(doc);
                }
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
            toReturn["timestamp"] = loggingEvent.TimeStamp.ToString(TIMESTAMP_FORMAT, DateTimeFormatInfo.InvariantInfo);
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

            // exception information
            if (loggingEvent.ExceptionObject != null)
            {
                toReturn["exception"] = ExceptionToBSON(loggingEvent.ExceptionObject);
            }
            return toReturn;
        }

        /// <summary>
        /// Create BSON representation of Exception
        /// Inner exceptions are handled recursively
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        protected Document ExceptionToBSON(Exception ex)
        {
            var toReturn = new Document();
            toReturn["message"] = ex.Message;
            toReturn["source"] = ex.Source;
            toReturn["stackTrace"] = ex.StackTrace;
            
            if (ex.InnerException != null)
            {
                toReturn["innerException"] = ExceptionToBSON( ex.InnerException);
            }

            return toReturn;
        }
    }
}
