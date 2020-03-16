using System.Collections.Generic;
using System.Linq;

namespace Xyzies.Devices.Data.Common
{
    public class LazyLoadedResult<T> : LazyLoadParameters where T : class 
    {
        public LazyLoadedResult()
        {
            Result = Enumerable.Empty<T>();
        }

        public IEnumerable<T> Result { get; set; }

        public int Total { get; set; }
    }
}
