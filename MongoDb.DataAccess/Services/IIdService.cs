namespace MongoDb.DataAccess.Services
{
    public interface IIdService
    {
        IdDef IncrementAndReturn(string identifier, int increment = 1);
    }
}