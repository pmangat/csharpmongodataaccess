using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDb.DataAccess
{
    public interface IMongoRepository<T>
    {
        IQueryable<T> Entities { get; }
        void Insert(T document, bool validate = true);
        Task InsertAsync(T document, bool validate = true);

        bool Upsert(T document, bool validate = true, bool canOverwriteServer = true);

        Task<bool> UpsertAsync(T document, bool validate = true, bool canOverwriteServer = true);

        bool Update(Expression<Func<T, bool>> predicate, IDictionary<string, object> updates);
        Task<bool> UpdateAsync(Expression<Func<T, bool>> predicate, IDictionary<string, object> updates);

        void Delete(string id);
        Task DeleteAsync(string id);

        long DeleteWithFilter(FilterDefinition<BsonDocument> filter);

        Task DeleteWithFilterAsync(FilterDefinition<BsonDocument> filter);

        void DeleteAll();
        Task DeleteAllAsync();

        IEnumerable<T> Find(Expression<Func<T, bool>> predicate);

        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        IEnumerable<T> FindComplex(Expression<Func<T, bool>> predicate);

        IEnumerable<T> Find(FilterDefinition<BsonDocument> filter);

        T FindOneAndUpdate(Expression<Func<T, bool>> predicate, JsonUpdateDefinition<T> updateDefinition,
            bool isUpsert = true);

        Task<T> FindOneAndUpdateAsync(Expression<Func<T, bool>> predicate, JsonUpdateDefinition<T> updateDefinition,
            bool isUpsert = true);
    }
}