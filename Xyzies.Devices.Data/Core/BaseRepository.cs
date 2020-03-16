using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Xyzies.Devices.Data.Core
{
    /// <summary>
    /// Abstract implementation of repository
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TProvider"></typeparam>
    public abstract class BaseRepository<TKey, TEntity, TProvider>
        : IRepository<TKey, TEntity>, IDisposable
        where TProvider : class, IDisposable
        where TEntity : class, IEntity<TKey>
        where TKey : IEquatable<TKey>, IComparable<TKey>, IComparable
    {
        protected TProvider Context { get; private set; }
        protected IAccessPointProvider<TProvider> AccessPoint { get; private set; }

        protected BaseRepository(IAccessPointProvider<TProvider> accessPointProvider)
        {
            AccessPoint = accessPointProvider ?? throw new ArgumentNullException(nameof(accessPointProvider));
            Context = AccessPoint.Provider ?? throw new ArgumentNullException(nameof(AccessPoint.Provider));
        }

        #region Disposable

        /// <inheritdoc />
        public virtual void Dispose()
        {
            if (this.AccessPoint != null)
            {
                this.AccessPoint.Dispose();
                this.AccessPoint = null;
            }
        }

        #endregion

        /// <inheritdoc />
        public abstract IQueryable<TEntity> Get();

        /// <inheritdoc />
        public abstract Task<IQueryable<TEntity>> GetAsync();

        /// <inheritdoc />
        public abstract TEntity Get(TKey id);

        /// <inheritdoc />
        public abstract IQueryable<TEntity> Get(Expression<Func<TEntity, bool>> predicate);

        /// <inheritdoc />
        public abstract TEntity GetBy(Expression<Func<TEntity, bool>> predicate);

        /// <inheritdoc />
        public abstract bool Has(TKey id);

        /// <inheritdoc />
        public abstract bool Has(TEntity entity);

        /// <inheritdoc />
        public abstract bool Has(Expression<Func<TEntity, bool>> predicate);

        /// <inheritdoc />
        public abstract TKey Add(TEntity entity);

        /// <inheritdoc />
        public abstract void AddRange(IEnumerable<TEntity> entities);

        /// <inheritdoc />
        public abstract bool Update(TEntity entity);

        /// <inheritdoc />
        public abstract bool Remove(TEntity entity);

        /// <inheritdoc />
        public abstract bool Remove(TKey id);

        /// <inheritdoc />
        public abstract void RemoveAll();

        /// <inheritdoc />
        public abstract Task<TEntity> GetAsync(TKey id);

        /// <inheritdoc />
        public abstract Task<IQueryable<TEntity>> GetAsync(Expression<Func<TEntity, bool>> predicate);

        /// <inheritdoc />
        public abstract Task<TEntity> GetByAsync(Expression<Func<TEntity, bool>> predicate);

        /// <inheritdoc />
        public abstract Task<bool> HasAsync(TKey id);

        /// <inheritdoc />
        public abstract Task<bool> HasAsync(TEntity entity);

        /// <inheritdoc />
        public abstract Task<bool> HasAsync(Expression<Func<TEntity, bool>> predicate);

        /// <inheritdoc />
        public abstract Task<TKey> AddAsync(TEntity entity);

        /// <inheritdoc />
        public abstract Task AddRangeAsync(IEnumerable<TEntity> entities);

        /// <inheritdoc />
        public abstract Task<bool> UpdateAsync(TEntity entity);

        /// <inheritdoc />
        public abstract Task UpdateRangeAsync(IEnumerable<TEntity> entities);

        /// <inheritdoc />
        public abstract Task<bool> RemoveAsync(TEntity entity);

        /// <inheritdoc />
        public abstract Task<bool> RemoveAsync(TKey id);

        /// <inheritdoc />
        public abstract Task RemoveAllAsync();
    }
}
