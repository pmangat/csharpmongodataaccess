using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDb.DataAccess.Services;
using MongoDB.Driver;

namespace MongoDb.DataAccess.Tests
{
    [TestClass]
    public class IdServiceTests : TestBase
    {
        private static IIdService idService;
        private static IMongoRepository<IdDef> counterRepo;

        public IdServiceTests()
        {
            Init();
            idService = container.Resolve<IIdService>();
            counterRepo = mongoCoreDbContext.GetRepository<IdDef>();
        }

        [TestInitialize]
        public void MyTestInitialize()
        {
            counterRepo.DeleteAll();

            //we have to intialize (seed) the counter document first otherwise the first simultaneous updates (may be threads) could create
            //more than one document per counter the very first time.
            counterRepo.FindOneAndUpdate(counter => counter.Identifier == "orderId",
                new JsonUpdateDefinition<IdDef>("{ $inc: { Value: 0}}"));

            counterRepo.FindOneAndUpdate(counter => counter.Identifier == "jobId",
                new JsonUpdateDefinition<IdDef>("{ $inc: { Value: 0}}"));
        }

        [TestMethod]
        public void can_test_counter()
        {
            var tasks = new Task[5];
            var factory = new TaskFactory();
            for (var i = 0; i <= 4; i++)
                tasks[i] = factory.StartNew(() =>
                {
                    var ret = idService.IncrementAndReturn("orderId");

                    return ret.Value;
                });

            Task.WaitAll(tasks);

            var currValue = counterRepo.Find(a => a.Identifier == "orderId").FirstOrDefault();
            currValue.Should().NotBeNull();
            currValue.Value.Should().Be(5);
        }

        [TestMethod]
        public void can_test_multiple_counters()
        {
            var tasks = new List<Task>();
            var factory = new TaskFactory();
            for (var i = 0; i <= 4; i++)
                tasks.Add(factory.StartNew(() =>
                {
                    idService.IncrementAndReturn("orderId", 2);
                    idService.IncrementAndReturn("jobId");
                }));

            Task.WaitAll(tasks.ToArray());

            idService.IncrementAndReturn("jobId");

            var currValue = counterRepo.Find(a => a.Identifier == "orderId").FirstOrDefault();
            currValue.Should().NotBeNull();
            currValue.Value.Should().Be(10);

            currValue = counterRepo.Find(a => a.Identifier == "jobId").FirstOrDefault();
            currValue.Should().NotBeNull();
            currValue.Value.Should().Be(6);
        }
    }
}