using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDb.DataAccess
{
    [Serializable]
    public abstract class EntityBase
    {
        //Object Id of Bson as a string in POCO.
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonRequired]
        public string Id { get; set; }


        //http://alexmg.com/datetime-precision-with-mongodb-and-the-c-driver/
        [BsonDateTimeOptions(Representation = BsonType.Document)]
        public DateTime LastUpdateDateTime { get; set; }
    }
}