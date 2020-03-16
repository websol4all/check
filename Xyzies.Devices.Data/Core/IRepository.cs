using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Xyzies.Devices.Data.Core
{
    /// <summary>
    /// Behaviour for the repository pattern.
    /// Provides the basic CRUD operations
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TEntity"></typeparam>
    public interface IRepository<TKey, TEntity> : IDisposable
        where TEntity : class, IEntity<TKey>
        where TKey : IEquatable<TKey>, IComparable<TKey>, IComparable
    {
        /// <summary>
        /// Get all entities
        /// </summary>
        /// <returns></returns>
        IQueryable<TEntity> Get();

        /// <summary>
        /// Get all entities
        /// </summary>
        /// <returns></returns>
        Task<IQueryable<TEntity>> GetAsync();

        /// <summary>
        /// Get entity by key
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        TEntity Get(TKey id);

        /// <summary>
        /// Get entity by key
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TEntity> GetAsync(TKey id);

        /// <summary>
        /// Get entities by expression
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        IQueryable<TEntity> Get(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Get entities by expression
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        Task<IQueryable<TEntity>> GetAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Get entity by expression
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        TEntity GetBy(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Get entity by expression
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        Task<TEntity> GetByAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Check contains the entity
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        bool Has(TKey id);

        /// <summary>
        /// Check contains the entity
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<bool> HasAsync(TKey id);

        /// <summary>
        /// Check contains the entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        bool Has(TEntity entity);

        /// <summary>
        /// Check contains the entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<bool> HasAsync(TEntity entity);

        /// <summary>
        /// Check contains the entity by expression
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        bool Has(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Check contains the entity by expression
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        Task<bool> HasAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Add new entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        TKey Add(TEntity entity);

        /// <summary>
        /// Add new entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<TKey> AddAsync(TEntity entity);

        /// <summary>
        /// Add range of new entities
        /// </summary>
        /// <param name="entities"></param>
        void AddRange(IEnumerable<TEntity> entities);

        /// <summary>
        /// Add range of new entities
        /// </summary>
        /// <param name="entities"></param>
        Task AddRangeAsync(IEnumerable<TEntity> entities);

        /// <summary>
        /// Update current entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        bool Update(TEntity entity);

        /// <summary>
        /// Update current entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<bool> UpdateAsync(TEntity entity);

        /// <summary>
        /// Update range of entities
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        Task UpdateRangeAsync(IEnumerable<TEntity> entities);

        /// <summary>
        /// Remove by entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        bool Remove(TEntity entity);

        /// <summary>
        /// Remove by entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<bool> RemoveAsync(TEntity entity);

        /// <summary>
        /// Remove by key
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        bool Remove(TKey id);

        /// <summary>
        /// Remove by key
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<bool> RemoveAsync(TKey id);

        /// <summary>
        /// Remove all entities
        /// </summary>
        void RemoveAll();

        /// <summary>
        /// Remove all entities
        /// </summary>
        Task RemoveAllAsync();
    }
}
