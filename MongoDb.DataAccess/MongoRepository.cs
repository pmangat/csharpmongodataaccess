using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Endjin.Core.Retry;
using Endjin.Core.Retry.Policies;
using MongoDb.DataAccess.Exceptions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using ValidationException = MongoDb.DataAccess.Exceptions.ValidationException;

namespace MongoDb.DataAccess
{
    /// <summary>
    ///     Generic implementation of repository pattern for Mongo.
    ///     Retries are implemented for all writes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MongoRepository<T> : IMongoRepository<T> where T : EntityBase
    {
        private readonly IMongoCollection<T> collection;
        private readonly IMongoDbContext context;

        internal MongoRepository(IMongoDbInternalContext context)
        {
            this.context = context;
            collection = context.GetCollection<T>();
        }

        public void Insert(T document, bool validate = true)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (validate)
            {
                var errors = new List<ValidationResult>();
                if (!Validate(document, ref errors))
                    throw new ValidationException(errors);
            }

            try
            {
                document.LastUpdateDateTime = DateTime.UtcNow;

                RetryTask.Factory.StartNew(
                    () => collection.WithWriteConcern(WriteConcern.WMajority).InsertOne(
                        document, new InsertOneOptions {BypassDocumentValidation = !validate}),
                    CancellationToken.None, WriteRetryStrategy.Create(), new AnyException());
            }
            catch (Exception exception)
            {
                throw new MongoDataAccessException(exception.Message);
            }
        }

        public async Task InsertAsync(T document, bool validate = true)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (validate)
            {
                var errors = new List<ValidationResult>();
                if (!Validate(document, ref errors))
                    throw new ValidationException(errors);
            }

            try
            {
                document.LastUpdateDateTime = DateTime.UtcNow;

                await RetryTask.Factory.StartNew(
                    () => collection.WithWriteConcern(WriteConcern.WMajority).InsertOneAsync(
                        document, new InsertOneOptions {BypassDocumentValidation = !validate}),
                    CancellationToken.None, WriteRetryStrategy.Create(), new AnyException());
            }
            catch (Exception exception)
            {
                throw new MongoDataAccessException(exception.Message);
            }
        }

        public bool Upsert(T document, bool validate = true, bool canOverwriteServer = true)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (validate)
            {
                var errors = new List<ValidationResult>();
                if (!Validate(document, ref errors))
                    throw new ValidationException(errors);
            }

            var currUpdateTime = document.LastUpdateDateTime;
            document.LastUpdateDateTime = DateTime.UtcNow;

            ReplaceOneResult result;

            if (!canOverwriteServer)
                try
                {
                    result = RetryTask<ReplaceOneResult>.Factory.StartNew(
                        () => collection.WithWriteConcern(WriteConcern.WMajority).ReplaceOne(arg =>
                                    (arg.Id == document.Id) && (arg.LastUpdateDateTime == currUpdateTime),
                            document,
                            new UpdateOptions {IsUpsert = true, BypassDocumentValidation = !validate}),
                        CancellationToken.None, WriteRetryStrategy.Create(), new WriteRetryPolicy()).Result;
                }
                catch (Exception exception)
                    //as overWriteServerChanges was false, we are trying for optimistic concurrency the 
                    //query part of the replace method will return 0 docs as the LastUpdateDateTime is 
                    //not what we expect (that is a case where the state is different than expected). 
                    //Though that means because of upsert flag in Mongo will try to insert, but document with 
                    //id is already present and so Mongo server throws MongoDuplicateKeyException. 
                    //So in this situation it is safe to throw the concurrency exception.
                {
                    var writeException = exception.InnerException?.InnerException as MongoWriteException;
                    //as tasks are going to throw aggregate exception.
                    if ((writeException != null) &&
                        (writeException.WriteError.Category == ServerErrorCategory.DuplicateKey))
                        throw new MongoConcurrencyException(document.Id);

                    throw new MongoDataAccessException(exception.Message);
                }
            else
                try
                {
                    result =
                        RetryTask<ReplaceOneResult>.Factory.StartNew(
                            () => collection.WithWriteConcern(WriteConcern.WMajority).ReplaceOne(
                                doc => doc.Id == document.Id,
                                document,
                                new UpdateOptions {IsUpsert = true, BypassDocumentValidation = !validate}),
                            CancellationToken.None, WriteRetryStrategy.Create(), new AnyException()).Result;
                }
                catch (Exception exception)
                {
                    throw new MongoDataAccessException(exception.Message);
                }

            return result.IsAcknowledged;
        }

        public async Task<bool> UpsertAsync(T document, bool validate = true, bool canOverwriteServer = true)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (validate)
            {
                var errors = new List<ValidationResult>();
                if (!Validate(document, ref errors))
                    throw new ValidationException(errors);
            }

            var currUpdateTime = document.LastUpdateDateTime;
            document.LastUpdateDateTime = DateTime.UtcNow;

            ReplaceOneResult result;
            if (!canOverwriteServer)
                try
                {
                    var res1 = await RetryTask<Task<ReplaceOneResult>>.Factory.StartNew(
                        () => collection.WithWriteConcern(WriteConcern.WMajority).ReplaceOneAsync(arg =>
                                    (arg.Id == document.Id) && (arg.LastUpdateDateTime == currUpdateTime),
                            document,
                            new UpdateOptions {IsUpsert = true, BypassDocumentValidation = !validate},
                            CancellationToken.None), WriteRetryStrategy.Create(), new WriteRetryPolicy());

                    result = res1.Result;
                }
                catch (Exception exception)
                    //as overWriteServerChanges was false, we are trying for optimistic concurrency the 
                    //query part of the replace method will return 0 docs as the LastUpdateDateTime is 
                    //not what we expect (that is a case where the state is different than expected). 
                    //Though that means because of upsert flag in Mongo will try to insert, but document with 
                    //id is already present and so Mongo server throws MongoDuplicateKeyException. 
                    //So in this situation it is safe to throw the concurrency exception.
                {
                    var writeException = exception.InnerException as MongoWriteException;
                    //as tasks are going to throw aggregate exception.
                    if ((writeException != null) &&
                        (writeException.WriteError.Category == ServerErrorCategory.DuplicateKey))
                        throw new MongoConcurrencyException(document.Id);

                    throw new MongoDataAccessException(exception.Message);
                }
            else
                try
                {
                    var res2 = await RetryTask<Task<ReplaceOneResult>>.Factory.StartNew(
                        () => collection.WithWriteConcern(WriteConcern.WMajority).ReplaceOneAsync(arg =>
                                    arg.Id == document.Id,
                            document,
                            new UpdateOptions {IsUpsert = true, BypassDocumentValidation = !validate},
                            CancellationToken.None), WriteRetryStrategy.Create(), new AnyException());
                    result = res2.Result;
                }
                catch (Exception exception)
                {
                    throw new MongoDataAccessException(exception.Message);
                }
            return result.IsAcknowledged;
        }

        public bool Update(Expression<Func<T, bool>> predicate, IDictionary<string, object> updates)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            if (updates == null)
                throw new ArgumentNullException(nameof(updates));

            if (updates.Count == 0)
                throw new ArgumentException("no values to update");

            try
            {
                updates.Add("LastUpdateDateTime", DateTime.UtcNow);
                var options = new UpdateOptions {IsUpsert = false};
                var updateDefBuilder = new UpdateDefinitionBuilder<T>();
                var updateList =
                    updates.Select(pair => updateDefBuilder.Set(pair.Key, pair.Value)).ToList();
                var updateDef = updateDefBuilder.Combine(updateList);

                var updateResult =
                    RetryTask<UpdateResult>.Factory.StartNew(
                        () =>
                            collection.WithWriteConcern(WriteConcern.WMajority)
                                .UpdateMany(predicate, updateDef, options), CancellationToken.None,
                        WriteRetryStrategy.Create(), new AnyException()).Result;

                return updateResult.IsAcknowledged;
            }
            catch (Exception exception)
            {
                throw new MongoDataAccessException(exception.Message);
            }
        }

        public async Task<bool> UpdateAsync(Expression<Func<T, bool>> predicate, IDictionary<string, object> updates)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            if (updates == null)
                throw new ArgumentNullException(nameof(updates));

            if (updates.Count == 0)
                throw new ArgumentException("no values to update");

            try
            {
                updates.Add("LastUpdateDateTime", DateTime.UtcNow);
                var options = new UpdateOptions {IsUpsert = false};
                var updateDefBuilder = new UpdateDefinitionBuilder<T>();
                var updateList =
                    updates.Select(pair => updateDefBuilder.Set(pair.Key, pair.Value)).ToList();
                var updateDef = updateDefBuilder.Combine(updateList);

                var updateResult =
                    await RetryTask<Task<UpdateResult>>.Factory.StartNew(
                        () =>
                            collection.WithWriteConcern(WriteConcern.WMajority)
                                .UpdateManyAsync(predicate, updateDef, options), CancellationToken.None,
                        WriteRetryStrategy.Create(), new AnyException());

                return updateResult.Result.IsAcknowledged;
            }
            catch (Exception exception)
            {
                throw new MongoDataAccessException(exception.Message);
            }
        }

        public void Delete(string id)
        {
            try
            {
                var deletedEntity =
                    RetryTask<T>.Factory.StartNew(() => collection.WithWriteConcern(WriteConcern.WMajority)
                            .FindOneAndDelete(obj => obj.Id == id.ToString()), CancellationToken.None,
                        WriteRetryStrategy.Create(), new AnyException()).Result;
            }
            catch (Exception exception)
            {
                throw new MongoDataAccessException(exception.Message);
            }
        }

        public async Task DeleteAsync(string id)
        {
            try
            {
                await RetryTask<Task<T>>.Factory.StartNew(() => collection.WithWriteConcern(WriteConcern.WMajority)
                        .FindOneAndDeleteAsync(arg => arg.Id == id.ToString()),
                    WriteRetryStrategy.Create(), new AnyException());
            }
            catch (Exception exception)
            {
                throw new MongoDataAccessException(exception.Message);
            }
        }

        public long DeleteWithFilter(FilterDefinition<BsonDocument> filter)
        {
            try
            {
                var result =
                    RetryTask<DeleteResult>.Factory.StartNew(
                        () =>
                            ((IMongoDbInternalContext) context).GetBsonCollection<T, BsonDocument>()
                                .WithWriteConcern(WriteConcern.WMajority)
                                .DeleteMany(filter), CancellationToken.None,
                        WriteRetryStrategy.Create(), new AnyException()).Result;

                return result.DeletedCount;
            }
            catch (Exception exception)
            {
                throw new MongoDataAccessException(exception.Message);
            }
        }

        public async Task DeleteWithFilterAsync(FilterDefinition<BsonDocument> filter)
        {
            try
            {
                await
                    RetryTask<Task<DeleteResult>>.Factory.StartNew(
                        () =>
                            ((IMongoDbInternalContext) context).GetBsonCollection<T, BsonDocument>()
                                .WithWriteConcern(WriteConcern.WMajority)
                                .DeleteManyAsync(filter), CancellationToken.None,
                        WriteRetryStrategy.Create(), new AnyException());
            }
            catch (Exception exception)
            {
                throw new MongoDataAccessException(exception.Message);
            }
        }

        public void DeleteAll()
        {
            try
            {
                var filter = new BsonDocument();

                var deleteResult = RetryTask<DeleteResult>.Factory.StartNew(() => collection.DeleteMany(filter),
                    CancellationToken.None, WriteRetryStrategy.Create(), new AnyException()).Result;
            }
            catch (Exception exception)
            {
                throw new MongoDataAccessException(exception.Message);
            }
        }

        public async Task DeleteAllAsync()
        {
            try
            {
                var filter = new BsonDocument();

                await RetryTask<Task<DeleteResult>>.Factory.StartNew(() => collection.DeleteManyAsync(filter),
                    CancellationToken.None, WriteRetryStrategy.Create(), new AnyException());
            }
            catch (Exception exception)
            {
                throw new MongoDataAccessException(exception.Message);
            }
        }

        public IQueryable<T> Entities => collection.AsQueryable();

        public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                throw new ValidationException(
                    new List<ValidationResult> {new ValidationResult("Null predicate not allowed")});

            return collection.Find(predicate).ToList();
        }

        public IEnumerable<T> Find(FilterDefinition<BsonDocument> filter)
        {
            IList<BsonDocument> docs =
                ((IMongoDbInternalContext) context).GetBsonCollection<T, BsonDocument>().Find(filter).ToList();
            //deserialization could be expensive.
            return docs.Select(doc => BsonSerializer.Deserialize<T>(doc));
        }

        //This needs to be removed until a better way to pass in complex queries are determined.
        public IEnumerable<T> FindComplex(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                throw new ValidationException(
                    new List<ValidationResult> {new ValidationResult("Null predicate not allowed")});

            var func = predicate.Compile();

            return collection.AsQueryable().Where(func);
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                throw new ValidationException(
                    new List<ValidationResult> {new ValidationResult("Null predicate not allowed")});

            return await collection.Find(predicate).ToListAsync();
        }

        public T FindOneAndUpdate(Expression<Func<T, bool>> predicate, JsonUpdateDefinition<T> updateDefinition,
            bool isUpsert = true)
        {
            var options = new FindOneAndUpdateOptions<T>
            {
                IsUpsert = isUpsert,
                ReturnDocument = ReturnDocument.After
            };

            var returnedDoc =
                RetryTask<T>.Factory.StartNew(() => collection.FindOneAndUpdate(predicate, updateDefinition, options),
                    CancellationToken.None, WriteRetryStrategy.Create(), new AnyException()).Result;

            return returnedDoc;
        }

        public async Task<T> FindOneAndUpdateAsync(Expression<Func<T, bool>> predicate,
            JsonUpdateDefinition<T> updateDefinition,
            bool isUpsert = true)
        {
            var options = new FindOneAndUpdateOptions<T>
            {
                IsUpsert = isUpsert,
                ReturnDocument = ReturnDocument.After
            };

            var returnedDoc =
                await
                    RetryTask<Task<T>>.Factory.StartNew(
                        () =>
                            collection.WithWriteConcern(WriteConcern.WMajority)
                                .FindOneAndUpdateAsync(predicate, updateDefinition, options),
                        CancellationToken.None, WriteRetryStrategy.Create(), new AnyException());

            return returnedDoc.Result;
        }

        private bool Validate(T document, ref List<ValidationResult> errors)
        {
            try
            {
                var validationContext = new ValidationContext(document, null, null);

                var isValid = Validator.TryValidateObject(document, validationContext, errors, true);

                return isValid;
            }
            catch (Exception exception)
            {
                throw new MongoDataAccessException(exception.Message);
            }
        }
    }
}