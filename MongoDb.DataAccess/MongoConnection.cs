using MongoDB.Driver;

namespace MongoDb.DataAccess
{
    public class MongoConnection : IMongoConnection
    {
        private readonly MongoClient mongoClient;

        /// <summary>
        ///     This method takes mongo connections string url
        ///     Format: mongodb://[username:password@]host1[:port1][,host2[:port2],...[,hostN[:portN]]][/[database][?options]]
        /// </summary>
        public MongoConnection(string mongoDbConnectionString)
        {
            //Mongo client need not be closed - when it is created a pool of connections is allocated and those should stay open.
            //http://mongodb.github.io/mongo-csharp-driver/2.0/getting_started/quick_tour/ (Make a connection section).

            //The MongoClient instance actually represents a pool of connections to the database; 
            //you will only need one instance of class MongoClient even with multiple threads.
            //important: Typically you only create one MongoClient instance for a given cluster and use it across your application.
            //Creating multiple MongoClients will, however, still share the same pool of connections if and only if the connection strings are identical.
            mongoClient = new MongoClient(mongoDbConnectionString);
        }

        public IMongoClient MongoClient => mongoClient;
    }
}