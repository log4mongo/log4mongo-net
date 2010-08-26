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

using MongoDB;
using NUnit.Framework;
using log4net;
using log4net.Appender;
using System;

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

            // Use mongo collection configured at appender level for tests
            collection = appender.LogCollection;
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

            var retrieved = collection.FindOne(null);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(retrieved["message"], "Oh, Mongo !");
            Assert.AreEqual(retrieved["loggerName"], typeof(MongoDBAppenderTests).FullName);
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

        [Test]
        public void TestException()
        {
            var ex = new Exception("Something wrong happened", new Exception("I'm the inner"));
            log.Error("I'm sorry", ex);
            Assert.AreEqual(1L, GetCollectionCount());

            var retrieved = collection.FindOne(null);
            Assert.IsNotNull(retrieved);

            // level
            Assert.AreEqual(retrieved["level"], "ERROR", "Exception not logged with ERROR level");
            
            // exception
            var exception = retrieved["exception"] as Document;
            Assert.IsNotNull(exception, "Log event does not contain expected exception");
            Assert.AreEqual(exception["message"], "Something wrong happened", "Exception message different from expected");

            // inner exception
            var innerException = exception["innerException"] as Document;
            Assert.IsNotNull(innerException, "Log event does not contain expected inner exception");
            Assert.AreEqual(innerException["message"], "I'm the inner", "Inner exception message different from expected");
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
