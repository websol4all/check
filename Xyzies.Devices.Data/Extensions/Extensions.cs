using System.Collections.Generic;
using System.Linq;
using Xyzies.Devices.Data.Common;

namespace Xyzies.Devices.Data.Extensions
{
    public static class Extensions
    {
        public static LazyLoadedResult<T> GetPart<T>(this IQueryable<T> query, LazyLoadParameters parameters)
                    where T : class
        {
            int count = query.Count();
            if (parameters == null)
            {
                return new LazyLoadedResult<T>
                {
                    Result = query,
                    Total = query.Count()
                };
            }
            var result = query.Skip(parameters.Offset.HasValue ? parameters.Offset.Value : 0);
            result = parameters.Limit.HasValue? result.Take(parameters.Limit.Value): result;

            return new LazyLoadedResult<T>
            {
                Result = result,
                Total = query.Count(),
                Offset = parameters.Offset,
                Limit = parameters.Limit
            };
        }

        public static LazyLoadedResult<T> GetPart<T>(this IEnumerable<T> query, LazyLoadParameters parameters)
                   where T : class
        {
            int count = query.Count();
            if (parameters == null)
            {
                return new LazyLoadedResult<T>
                {
                    Result = query,
                    Total = query.Count()
                };
            }
            var result = query.Skip(parameters.Offset.HasValue ? parameters.Offset.Value : 0);
            result = parameters.Limit.HasValue ? result.Take(parameters.Limit.Value) : result;

            return new LazyLoadedResult<T>
            {
                Result = result,
                Total = query.Count(),
                Offset = parameters.Offset,
                Limit = parameters.Limit
            };
        }
    }
}
