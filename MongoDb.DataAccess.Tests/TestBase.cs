using System;
using Castle.Windsor;
using MongoDb.DataAccess.DI;
using MongoDb.DataAccess.Services;

namespace MongoDb.DataAccess.Tests
{
    public abstract class TestBase : IDisposable
    {
        protected readonly IWindsorContainer container = new WindsorContainer();
        private IMongoDbContextFactory mongoContextFactory;
        protected IMongoDbContext mongoCoreDbContext;
        protected IMongoDbContext mongoTestDbContext;

        public void Dispose()
        {
            mongoContextFactory.Release(mongoTestDbContext);
        }

        protected void Init()
        {
            container.Install(new MongoDataAccessInstaller());

            mongoContextFactory = container.Resolve<IMongoDbContextFactory>();

            mongoTestDbContext = mongoContextFactory.Create("TestDb");
            mongoCoreDbContext = mongoContextFactory.Create(IdService.CoreDbName);
        }
    }
}