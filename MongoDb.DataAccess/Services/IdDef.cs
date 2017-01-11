namespace MongoDb.DataAccess.Services
{
    [Collection(Name = "Identifiers")]
    public class IdDef : EntityBase
    {
        public string Identifier { get; set; }
        public int Value { get; set; }
    }
}