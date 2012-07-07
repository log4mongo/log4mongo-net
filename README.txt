log4mongo-net
===============
log4mongo-net is log4net appender to MongoDB database.
log4mongo-net is using 10gen official C# driver -  http://www.mongodb.org/display/DOCS/CSharp+Language+Center

Licence
============
New BSD License

Requirements
============
- .NET 3.5+ required
- tested with MongoDB 1.2+

Configuration
=============
Include all DLLs in bin/ directory to your project.

log4net appender sample XML configuration::

<appender name="MongoAppender" type="Log4MongoAppender, log4mongo-net">
  <!-- MongoDB connection options -->
  <host value="localhost" />
  <port value="27017" />
  <databaseName value="logs" />
  <collectionName value="logs_net" />
  <!-- 
  Uncomment following for MongoDB authentication
  See http://www.mongodb.org/display/DOCS/Security+and+Authentication
  <userName value="mylogin" />
  <password value="mysecretpass" />
  -->
</appender>

Author
======
Jozef Sevcik <sevcik@codescale.net>

References
==========
[1] http://www.mongodb.org/
[2] http://logging.apache.org/log4net/
[3] http://www.mongodb.org/display/DOCS/CSharp+Language+Center
