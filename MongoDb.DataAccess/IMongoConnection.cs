using MongoDB.Driver;

namespace MongoDb.DataAccess
{
    public interface IMongoConnection
    {
        IMongoClient MongoClient { get; }
    }
}