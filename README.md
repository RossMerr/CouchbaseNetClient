# CouchbaseNetClient

[![Build status](https://ci.appveyor.com/api/projects/status/ne6pnf0ed5114yey?svg=true)](https://ci.appveyor.com/project/rossmerr/couchbasenetclient)

https://www.myget.org/F/caudex/api/v3/index.json

A Build of the CouchbaseNetClient for dot.net core

### Linux ###

Under linux dotnet core does not support socket keep alive, so under the ClientConfiguration you need to set the EnableTcpKeepAlive to false.
