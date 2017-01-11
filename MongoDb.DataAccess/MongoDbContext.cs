using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Castle.Core.Internal;
using MongoDB.Driver;
using ValidationException = MongoDb.DataAccess.Exceptions.ValidationException;

namespace MongoDb.DataAccess
{
    public class MongoDbContext : IMongoDbInternalContext
    {
        private readonly IMongoConnection mongoConnection;
        private readonly IMongoDatabase mongoDatabase;

        public MongoDbContext(IMongoConnection connection, string mongoDbName)
        {
            mongoConnection = connection;
            //Mongo client need not be closed - when it is created a pool of connections is allocated and those should stay open.
            //http://mongodb.github.io/mongo-csharp-driver/2.0/getting_started/quick_tour/ (Make a connection section).
            mongoDatabase = mongoConnection.MongoClient.GetDatabase(mongoDbName);
        }

        public IMongoRepository<T> GetRepository<T>() where T : EntityBase
        {
            ParseCollectionAttribute<T>();
            return new MongoRepository<T>(this);
        }

        public IMongoCollection<T> GetCollection<T>()
        {
            return mongoDatabase.GetCollection<T>(ParseCollectionAttribute<T>());
        }

        public IMongoCollection<BsonDocument> GetBsonCollection<T, BsonDocument>()
        {
            return mongoDatabase.GetCollection<BsonDocument>(ParseCollectionAttribute<T>());
        }

        private string ParseCollectionAttribute<T>()
        {
            MemberInfo info = typeof(T);
            var collectionAttribute = info.GetAttribute<CollectionAttribute>();
            if (collectionAttribute == null)
                throw new ValidationException(new List<ValidationResult>
                {
                    new ValidationResult("The class is not decorated with Collection attribute")
                });
            return collectionAttribute.Name;
        }
    }
}