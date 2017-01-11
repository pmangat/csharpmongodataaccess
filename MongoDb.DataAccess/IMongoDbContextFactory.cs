namespace MongoDb.DataAccess
{
    public interface IMongoDbContextFactory
    {
        IMongoDbContext Create(string mongoDbName);
        void Release(IMongoDbContext context);
    }
}