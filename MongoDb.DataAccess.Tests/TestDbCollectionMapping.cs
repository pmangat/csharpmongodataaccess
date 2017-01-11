using Advent.MongoDb.DataAccess.Mapping;
using Advent.MongoDb.DataAccess.Tests.Entities;

namespace Advent.MongoDb.DataAccess.Tests
{
    public class TestDbCollectionMapping : BaseCollectionMapping
    {
        public TestDbCollectionMapping()
        {
            AddMapping(typeof (Department), "Departments");
            AddMapping(typeof (Employee), "Employees");
        }
    }
}