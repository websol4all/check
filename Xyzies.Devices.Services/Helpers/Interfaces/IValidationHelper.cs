using System;
using System.Threading.Tasks;

namespace Xyzies.Devices.Services.Helpers.Interfaces
{
    /// <summary>
    /// IValidation helper
    /// </summary>
    public interface IValidationHelper
    {
        /// <summary>
        /// Get company id by permission
        /// </summary>
        /// <param name="token"></param>
        /// <param name="scopes"></param>
        /// <param name="companyId"></param>
        /// <returns></returns>
        Task<int?> GetCompanyIdByPermission(string token, string[] scopes, int? companyId = null);

        /// <summary>
        /// Validate company and branch
        /// </summary>
        /// <param name="companyId"></param>
        /// <param name="branchId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task ValidateCompanyAndBranch(int companyId, Guid branchId, string token);
    }
}
