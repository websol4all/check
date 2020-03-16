using Microsoft.EntityFrameworkCore.Storage;
using System;

namespace Xyzies.Devices.Data.Core
{
    /// <summary>
    /// Behaviour of unit of work wrapper
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Get the current database transaction
        /// </summary>
        IDbContextTransaction CurrentTransaction { get; }

        /// <summary>
        /// Commit all made changes into the context
        /// </summary>
        void Commit();

        /// <summary>
        /// Rollback all made changes into the context
        /// </summary>
        void Rollback();
    }
}
