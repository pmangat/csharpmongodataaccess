using System;

namespace MongoDb.DataAccess.Exceptions
{
    public class MongoDataAccessException : ApplicationException
    {
        public MongoDataAccessException(string message) : base(message)
        {
        }
    }
}