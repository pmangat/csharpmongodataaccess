using System.Configuration;
using Autofac;
using MongoDb.DataAccess.Services;

namespace MongoDb.DataAccess.DI
{
    public class MongoDataAccessModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(
                    cntx => new MongoConnection(ConfigurationManager.AppSettings["mongoDbConnectionString"]))
                .As<IMongoConnection>()
                .SingleInstance();

            builder.RegisterType<MongoDbContext>()
                .As<IMongoDbContext>()
                .As<IMongoDbInternalContext>()
                .InstancePerDependency();

            builder.RegisterType<IdService>()
                .As<IIdService>()
                .SingleInstance();
        }
    }
}