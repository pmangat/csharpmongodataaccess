using System.Linq;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using MongoDb.DataAccess.Services;

namespace MongoDb.DataAccess.DI
{
    public class MongoDataAccessInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            //Facilities.
            if (!container.Kernel.GetFacilities().Any(f => f is TypedFactoryFacility))
                container.AddFacility<TypedFactoryFacility>();

            container.Register(
                Component.For<IMongoConnection>()
                    .ImplementedBy<MongoConnection>()
                    .LifestyleSingleton()
                    .DependsOn(Dependency.OnAppSettingsValue("mongoDbConnectionString", "mongoDbConnectionString")));

            container.Register(Component.For<IMongoDbContextFactory>().AsFactory().LifestyleSingleton());
            container.Register(
                Component.For<IMongoDbContext, IMongoDbInternalContext>()
                    .ImplementedBy<MongoDbContext>()
                    .LifestyleTransient());

            container.Register(
                Component.For<IIdService>()
                    .ImplementedBy<IdService>().LifestyleSingleton());
        }
    }
}