using System;
using Endjin.Core.Retry.Policies;
using MongoDB.Driver;

namespace MongoDb.DataAccess
{
    public class WriteRetryPolicy : IRetryPolicy
    {
        public bool CanRetry(Exception exception)
        {
            var storageException = exception as MongoWriteException;

            if (storageException == null)
                return true;

            return false;
        }
    }
}