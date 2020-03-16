using System;

namespace Xyzies.Devices.Data.Core
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TProvider"></typeparam>
    public class AccessPointProvider<TProvider> : IAccessPointProvider<TProvider>, IDisposable
        where TProvider : class, IDisposable
    {
        private TProvider _provider = null;

        public TProvider Provider => this._provider;

        public AccessPointProvider(TProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public virtual void Dispose()
        {
            _provider.Dispose();
            _provider = null;
        }

        #region Helpers

        /// <summary>
        /// Create a new instance of access point provider
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static IAccessPointProvider<TProvider> Create(TProvider provider) =>
            new AccessPointProvider<TProvider>(provider);

        #endregion
    }
}
