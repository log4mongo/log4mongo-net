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

using MongoDB.Driver;
using NUnit.Framework;
using log4net;
using log4net.Appender;

namespace log4net_MongoDB.Tests
{
    [TestFixture]
    public class MongoDBAppenderTests
    {
        private static ILog log = LogManager.GetLogger(typeof(MongoDBAppenderTests));
        private MongoDBAppender appender;
        private IMongoCollection collection;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            log4net.Config.XmlConfigurator.Configure();
            var appenders = log.Logger.Repository.GetAppenders();
            Assert.IsTrue(appenders.Length > 0, "Seems that MongoDB Appender is not configured");
            
            appender = appenders[0] as MongoDBAppender;
            Assert.IsNotNull(appender, "MongoDBAppender is expected to be the only one configured for tests");

            /* 
             * Initiate connection to mongo
             * This connection will be used to check log events written by appender
             */
             var connection = new Mongo(appender.Host, appender.Port);
             connection.Connect();

             // TODO: support for authentication
             collection = connection.getDB(appender.DatabaseName).GetCollection(appender.CollectionName);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            ClearCollection();
            LogManager.Shutdown();
        }

        [SetUp]
        public void TestSetUp()
        {
            ClearCollection();
        }

        [Test]
        public void TestSingleEvent()
        {    
            log.Debug("Oh, Mongo !");
            Assert.AreEqual(1L, GetCollectionCount());
        }

        [Test]
        public void TestMultipleEvents()
        {
            const int numberOfEvents = 12;
            for(var i = 0; i < numberOfEvents; ++i)
            {
                log.Debug(i);
            }
            Assert.AreEqual(numberOfEvents, GetCollectionCount());
        }

        protected long GetCollectionCount()
        {
            return collection.Count();
        }

        protected void ClearCollection()
        {
            collection.Delete(new Document());
        }
    }
}
