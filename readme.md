MongoDB appender for log4net
----------------------------

The title says it all. Check [Log4Net site](http://logging.apache.org/log4net/) or [MongoDB site](http://www.mongodb.org/) if you need more info.

This is the official .NET implementation for the [log4mongo](http://log4mongo.org) project

To get started, check out [@sammleach](https://twitter.com/sammleach) blog post: [.NET Logging with log4mongo-net](http://samlea.ch/dev/log4mongo-net/)

Installation
------------

[Get it on NuGet](https://nuget.org/packages/log4mongo-net), or download sources and run build.cmd to build

Appender configuration sample
-----------------------------

	<appender name="MongoDBAppender" type="Log4Mongo.MongoDBAppender, Log4Mongo">
		<!-- 
		MongoDB database connection in the format:
		mongodb://[username:password@]host1[:port1][,host2[:port2],...[,hostN[:portN]]][/[database][?options]]
		See http://www.mongodb.org/display/DOCS/Connections for connectionstring options 
		If no database specified, default to "log4net"
		-->
		<connectionString value="mongodb://localhost" />
		<!-- 
		Name of connectionString defined in web/app.config connectionStrings group, the format is the same as connectionString value.
		Optional, If not provided will use connectionString value
		-->
		<connectionStringName value="mongo-log4net" />
		<!-- 
		Name of the collection in database
		Optional, Defaults to "logs"
		-->
		<collectionName value="logs" />

		<!--
		Maximum size of newly created collection. Optional, Defaults to creating uncapped collections
		-->
		<newCollectionMaxDocs value='5000' />
		<newCollectionMaxSize value='65536' />
		
		<field>
			<name value="timestamp" />
			<layout type="log4net.Layout.RawTimeStampLayout" />
		</field>
		<field>
			<name value="level" />
			<layout type="log4net.Layout.PatternLayout" value="%level" />
		</field>
		<field>
			<name value="thread" />
			<layout type="log4net.Layout.PatternLayout" value="%thread" />
		</field>
		<field>
			<name value="logger" />
			<layout type="log4net.Layout.PatternLayout" value="%logger" />
		</field>
		<field>
			<name value="message" />
			<layout type="log4net.Layout.PatternLayout" value="%message" />
		</field>
		<field>
			<name value="mycustomproperty" />
			<layout type="log4net.Layout.RawPropertyLayout">
				<key value="mycustomproperty" />
			</layout>
		</field>
	</appender>

Note about Default Write Concern Change in driver 1.7+
------------------------------------------------------

[10gen changed the default value for WriteConcern](http://blog.mongodb.org/post/36666163412/introducing-mongoclient). This change is [implemented in MongoDB C# driver starting from 1.7](http://docs.mongodb.org/manual/release-notes/drivers-write-concern/#releases) and is effective only when used with the new `MongoDB.Driver.MongoClient` class.

For logging concern, the old default is usually better so for now Log4Mongo will keep creating database connection in the old way (by using `MongoDB.Driver.MongoServer.Create`). At some point (maybe with a major release) we will switch and start using `MongoDB.Driver.MongoClient` class, so it's best if you explicitly specify WriteConcern related options in the connection string, as described here: [Write Concern Options](http://docs.mongodb.org/manual/reference/connection-string/#write-concern-options)

License
-------

[BSD 3](https://raw.github.com/log4mongo/log4mongo-net/master/LICENSE)

Credits
-------

Thanks to [JetBrains](http://www.jetbrains.com/) for providing us licenses for its excellent tool [ReSharper](http://www.jetbrains.com/resharper/)

![ReSharper](http://www.jetbrains.com/img/logos/logo_resharper_small.gif)
