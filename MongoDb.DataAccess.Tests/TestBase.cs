using System;
using Autofac;
using MongoDb.DataAccess.DI;
using MongoDb.DataAccess.Services;

namespace MongoDb.DataAccess.Tests
{
    public abstract class TestBase : IDisposable
    {
        protected IContainer container;
        protected IMongoDbContext mongoCoreDbContext;
        protected IMongoDbContext mongoTestDbContext;

        public void Dispose()
        {
            container.Dispose();
        }

        protected void Init()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new MongoDataAccessModule());
            container = builder.Build();

            mongoTestDbContext = container.Resolve<IMongoDbContext>(new NamedParameter("mongoDbName", "TestDb"));
            mongoCoreDbContext =
                container.Resolve<IMongoDbContext>(new NamedParameter("mongoDbName", IdService.CoreDbName));
        }
    }
}