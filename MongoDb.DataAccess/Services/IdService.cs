using System;
using MongoDB.Driver;

namespace MongoDb.DataAccess.Services
{
    public class IdService : IIdService
    {
        public static string CoreDbName = "CoreDb";
        private readonly IMongoRepository<IdDef> idDefRepository;

        public IdService(Func<string, IMongoDbContext> mongoDbContext)
        {
            var mongoContext = mongoDbContext(CoreDbName);
            idDefRepository = mongoContext.GetRepository<IdDef>();
        }

        public IdDef IncrementAndReturn(string identifier, int increment = 1)
        {
            var result = idDefRepository.FindOneAndUpdate(counter => counter.Identifier == identifier,
                new JsonUpdateDefinition<IdDef>($"{{ $inc: {{ Value: {increment}}}}}"));

            return result;
        }
    }
}