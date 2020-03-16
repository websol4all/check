using System;

namespace Xyzies.Devices.Data.Core
{
    public interface IAccessPointProvider<TProvider> : IDisposable where TProvider : class, IDisposable
    {
        /// <summary>
        /// Data access provider
        /// </summary>
        TProvider Provider { get; }
    }
}
