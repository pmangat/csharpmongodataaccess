using MongoDB.Bson;

namespace MongoDb.DataAccess.Tests.Entities
{
    [Collection(Name = "Employees")]
    public class Employee : EntityBase
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Address Address { get; set; }
        public BsonDocument CustomData { get; set; }
    }
}