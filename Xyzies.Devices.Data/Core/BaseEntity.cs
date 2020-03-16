using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Xyzies.Devices.Data.Core
{
    /// <summary>
    /// A base entity
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public abstract class BaseEntity<TKey> : IEntity<TKey>
        where TKey : IEquatable<TKey>, IComparable<TKey>, IComparable
    {
        /// <inheritdoc />
        [Key]
        public virtual TKey Id { get; set; }

        [DefaultValue(false)]
        public virtual bool IsDeleted { get; set; }

        /// <inheritdoc />
        protected BaseEntity()
        {
            this.Id = default(TKey);
        }

        /// <inheritdoc />
        public bool EqualsByKey(TKey key)
        {
            return this.Id.Equals(key);
        }

        /// <summary>
        /// Create a new instance of an entity
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns>A new instance of entity</returns>
        public static TEntity Create<TEntity>()
            where TEntity : IEntity<TKey>
        {
            TEntity instance = Activator.CreateInstance<TEntity>();
            instance.Id = default(TKey);

            return instance;
        }
    }
}
