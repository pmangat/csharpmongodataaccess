namespace MongoDb.DataAccess.Tests.Entities
{
    [Collection(Name = "Departments")]
    public class Department : EntityBase
    {
        public string Name { get; set; }
    }
}