using MongoDB.Driver;

namespace MongoDb.DataAccess
{
    internal interface IMongoDbInternalContext : IMongoDbContext
    {
        IMongoCollection<BsonDocument> GetBsonCollection<T, BsonDocument>();
        IMongoCollection<T> GetCollection<T>();
    }
}