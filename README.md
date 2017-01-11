# csharpmongodataaccess
The library gives basic methods for mongo data access (repository pattern) plus some more features like optimistic concurrency support.

Each entity (C# class) that you declare need to be derived from EntityBase. EntityBase gives every entity an id and update timestamp. The repository interface mandates the use of EntityBase. (See Entities folder under test).

An entity represents the document structure in Mongo - such entities can be marked by the Collection attribute. The repository will do CRUD against the collection in Mongo. Of course collections need not pre-exist in Mongo. The first insert will create the collection. 

When writes happen there could be a temporary unavailability of the primary due to fail over. So a retry mechanism is build in using Endjin.Retry package with a custom retry policy and strategy.

Optimistic concurrency is supported in the 'Upsert' document methods - we use the Mongo C# driver ReplaceOne / ReplaceOneAsync methods. The first argument is a query to see if the document exists - if the incoming document has an older timestamp the query will return no rows and ReplaceOne will try and insert. This would fail as document with 'Id' already exists - this means the document passed in is outdated and needs refresh. 
