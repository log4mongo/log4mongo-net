log4net-MongoDB
===============

log4net-MongoDB is log4net appender to MongoDB database.

log4net-MongoDB is using mongodb-csharp driver (http://github.com/samus/mongodb-csharp).


Requirements
============
- build with MSBuild/VS, .NET 3.5+ required
- tested against MongoDB 1.3.2


Configuration
=============
Example appender XML configuration::

<appender name="MongoAppender" type="log4net.Appender.MongoDBAppender, log4net-MongoDB">
<!-- MongoDB connection options -->
<host value="localhost" />
<port value="27017" />
<databaseName value="log4net_mongodb" />
<collectionName value="logs" />
</appender>


TODO
====
- Exception object is not logged yet
- support fot MongoDB authentication
- dist/bin directory for build outputs

Author
======
Jozef Sevcik <sevcik@styxys.com>

References
==========
[1] http://www.mongodb.org/

[2] http://logging.apache.org/log4net/

[3] http://github.com/samus/mongodb-csharp