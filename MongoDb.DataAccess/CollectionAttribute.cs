using System;

namespace MongoDb.DataAccess
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class CollectionAttribute : Attribute
    {
        public string Name { get; set; }
    }
}