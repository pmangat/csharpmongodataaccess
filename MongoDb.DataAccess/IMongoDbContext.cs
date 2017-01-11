namespace MongoDb.DataAccess
{
    public interface IMongoDbContext
    {
        IMongoRepository<T> GetRepository<T>() where T : EntityBase;
    }
}