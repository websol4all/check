using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xyzies.Devices.Data.Core;

namespace Xyzies.Devices.Data.Repository
{
    /// <summary>
    /// Provides the basic CRUD operations using Entity Framework Core
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TEntity"></typeparam>
    public abstract class EfCoreBaseRepository<TKey, TEntity>
        : BaseRepository<TKey, TEntity, DbContext>, IUnitOfWork
        where TEntity : class, IEntity<TKey>
        where TKey : IEquatable<TKey>, IComparable<TKey>, IComparable
    {
        /// <summary>
        /// Access to stored collection of entity
        /// </summary>
        protected DbSet<TEntity> Data { get; private set; }

        protected EfCoreBaseRepository(DbContext dbContext)
            : this(AccessPointProvider<DbContext>.Create(dbContext))
        {
            // For unit testing in memory db
            if (!dbContext.Database.ProviderName.Equals("Microsoft.EntityFrameworkCore.InMemory"))
            {
                // TODO: Attached-mode
                var dbConnection = dbContext.Database.GetDbConnection();

                // For attached scenario
                if (dbConnection.State == ConnectionState.Closed ||
                    dbConnection.State == ConnectionState.Broken)
                {
                    dbConnection.Open();
                }
            }
        }

        protected EfCoreBaseRepository(IAccessPointProvider<DbContext> accessPointProvider)
            : base(accessPointProvider) =>
                Data = base.Context.Set<TEntity>() ??
                    throw new InvalidOperationException("DbContext doesn't contains a selected entity");

        #region Implementation

        /// <inheritdoc />
        public override IQueryable<TEntity> Get() =>
            Data.AsQueryable<TEntity>();

        /// <inheritdoc />
        public override async Task<IQueryable<TEntity>> GetAsync() =>
            await Task.FromResult(Data.AsQueryable<TEntity>());

        /// <inheritdoc />
        public override TEntity Get(TKey id) =>
            Data.FirstOrDefault<TEntity>(entity => entity.Id.Equals(id));

        /// <inheritdoc />
        public override async Task<TEntity> GetAsync(TKey id) =>
            await Data.FirstOrDefaultAsync<TEntity>(entity => entity.Id.Equals(id));

        /// <inheritdoc />
        public override IQueryable<TEntity> Get(Expression<Func<TEntity, bool>> predicate) =>
            Data.Where(predicate);

        /// <inheritdoc />
        public override async Task<IQueryable<TEntity>> GetAsync(Expression<Func<TEntity, bool>> predicate) =>
            await Task.FromResult(Data.Where(predicate));

        /// <inheritdoc />
        public override TEntity GetBy(Expression<Func<TEntity, bool>> predicate) =>
            Data.FirstOrDefault(predicate);

        /// <inheritdoc />
        public override async Task<TEntity> GetByAsync(Expression<Func<TEntity, bool>> predicate) =>
            await Data.FirstOrDefaultAsync(predicate);

        /// <inheritdoc />
        public override bool Has(TKey id) =>
            Data.Any(entity => entity.Id.Equals(id));

        /// <inheritdoc />
        public override async Task<bool> HasAsync(TKey id) =>
            await Data.AnyAsync(entity => entity.Id.Equals(id));

        /// <inheritdoc />
        public override bool Has(TEntity entity) =>
            Data.Any(e => (e as IEquatable<TEntity>).Equals(entity));

        /// <inheritdoc />
        public override async Task<bool> HasAsync(TEntity entity) =>
            await Data.AnyAsync(e => (e as IEquatable<TEntity>).Equals(entity));

        /// <inheritdoc />
        public override bool Has(Expression<Func<TEntity, bool>> predicate) =>
            Data.Any(predicate);

        /// <inheritdoc />
        public override async Task<bool> HasAsync(Expression<Func<TEntity, bool>> predicate) =>
            await Data.AnyAsync(predicate);

        /// <inheritdoc />
        public override TKey Add(TEntity entity) =>
            Commit(() => Data.Add(entity).Entity.Id);

        /// <inheritdoc />
        public override async Task<TKey> AddAsync(TEntity entity) =>
            await Commit(async () => (await Data.AddAsync(entity)).Entity.Id);

        /// <inheritdoc />
        public override void AddRange(IEnumerable<TEntity> entities) =>
            Commit(() => Data.AddRange(entities));

        /// <inheritdoc />
        public override async Task AddRangeAsync(IEnumerable<TEntity> entities) =>
            await Commit(async () => await Data.AddRangeAsync(entities));

        /// <inheritdoc />
        public override bool Update(TEntity entity) =>
            Commit((Action)(() => Data.Update(entity)));

        /// <inheritdoc />
        public override async Task<bool> UpdateAsync(TEntity entity) =>
            await CommitAsync((Action)(() => Data.Update(entity)));

        /// <inheritdoc />
        public override async Task UpdateRangeAsync(IEnumerable<TEntity> entities) =>
            await CommitAsync((() => Data.UpdateRange(entities)));

        /// <inheritdoc />
        public override bool Remove(TEntity entity) =>
            Commit((Action)(() => Data.Remove(entity)));

        /// <inheritdoc />
        public override async Task<bool> RemoveAsync(TEntity entity) =>
            await CommitAsync((Action)(() => Data.Remove(entity)));

        /// <inheritdoc />
        public override bool Remove(TKey id) =>
            Remove(Get(id));

        /// <inheritdoc />
        public override async Task<bool> RemoveAsync(TKey id) =>
            await RemoveAsync(await GetAsync(id));

        /// <inheritdoc />
        public override void RemoveAll() =>
            Commit(() => Data.RemoveRange(Get()));

        /// <inheritdoc />
        public override async Task RemoveAllAsync() =>
            await CommitAsync(() => Data.RemoveRange(Get()));

        #endregion

        #region Unit of Work

        /// <inheritdoc />
        public IDbContextTransaction CurrentTransaction =>
            base.Context.Database.BeginTransaction();

        /// <inheritdoc />
        public void Commit() =>
            base.Context.Database.CommitTransaction();

        /// <inheritdoc />
        public void Rollback() =>
            base.Context.Database.RollbackTransaction();

        #endregion

        #region Helpers

        private bool Commit(Action action)
        {
            action.Invoke();
            base.Context.SaveChanges();

            return true;
        }

        private T Commit<T>(Func<T> action)
        {
            T result = action.Invoke();
            base.Context.SaveChanges();

            return result;
        }

        private async Task<bool> CommitAsync(Action action)
        {
            action.Invoke();
            await base.Context.SaveChangesAsync();

            return await Task.FromResult(true);
        }

        private async Task<T> CommitAsync<T>(Func<T> func)
        {
            var result = Task.FromResult(func.Invoke());
            await base.Context.SaveChangesAsync();

            return await result;
        }

        #endregion
    }
}
