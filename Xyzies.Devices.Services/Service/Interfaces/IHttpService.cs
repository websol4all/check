using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xyzies.Devices.Services.Models.Branch;
using Xyzies.Devices.Services.Models.Company;
using Xyzies.Devices.Services.Models.Tenant;
using Xyzies.Devices.Services.Models.User;

namespace Xyzies.Devices.Services.Service.Interfaces
{
    /// <summary>
    /// Http clien for communication with microservices
    /// </summary>
    public interface IHttpService
    {
        /// <summary>
        /// Get company by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<CompanyModel> GetCompanyById(int id, string token);

        /// <summary>
        /// Get companies by ids
        /// </summary>
        /// <param name="idsQeury"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<List<CompanyModel>> GetCompaniesByIds(string idsQeury, string token);

        /// <summary>
        /// Get tenant by ids
        /// </summary>
        /// <param name="idsQeury"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<List<TenantFullModel>> GetTenantsByIds(string idsQeury = null, string token = null);

        /// <summary>
        /// Get branch by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<BranchModel> GetBranchById(Guid id, string token);

        /// <summary>
        /// Get branches by ids
        /// </summary>
        /// <param name="idsQeury"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<List<BranchModel>> GetBranchesByIds(string idsQeury, string token);

        /// <summary>
        /// Get current user
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<UserModel> GetCurrentUser(string token);

        /// <summary>
        /// Get users by ids
        /// </summary>
        /// <param name="token"></param>
        /// <param name="idsQeury"></param>
        /// <returns></returns>
        Task<List<UserModel>> GetUsersByIdsAsync(string token, string idsQeury);

        /// <summary>
        /// Get users by ids
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        Task<List<UserModel>> GetUsersByIdTrustedAsync(string query);

        /// <summary>
        /// Get Branch by id
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        Task<List<BranchModel>> GetBranchesTrustedAsync();

        /// <summary>
        /// Send Alert Email
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        Task PostNotificationEmailAsync(string query = null);

        /// <summary>
        /// Get tenant single model by company id
        /// </summary>
        /// <param name="companyId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<TenantFullModel> GetTenantSingleByCompanyId(int companyId, string token);

        /// <summary>
        /// Returns a list of companies for trusted microservice by token
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        Task<List<CompanyModel>> GetCompaniesForTrustedAsync();

    }
}
