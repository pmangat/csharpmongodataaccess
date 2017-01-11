using System;

namespace MongoDb.DataAccess.Exceptions
{
    public class MongoConcurrencyException : ApplicationException
    {
        public MongoConcurrencyException(string message) : base(message)
        {
        }
    }
}