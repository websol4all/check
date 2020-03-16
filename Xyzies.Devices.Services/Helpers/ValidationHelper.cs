using IdentityServiceClient.Service;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xyzies.Devices.Services.Helpers.Interfaces;
using Xyzies.Devices.Services.Service.Interfaces;

namespace Xyzies.Devices.Services.Helpers
{
    /// <inheritdoc />
    public class ValidationHelper : IValidationHelper
    {
        private readonly ILogger<ValidationHelper> _logger = null;
        private readonly IHttpService _httpService = null;
        private readonly IIdentityManager _identityManager = null;

        /// <summary>
        /// Validation helper constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="httpService"></param>
        /// <param name="identityManager"></param>
        public ValidationHelper(ILogger<ValidationHelper> logger,
            IHttpService httpService,
            IIdentityManager identityManager)
        {
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _httpService = httpService ??
                throw new ArgumentNullException(nameof(httpService));
            _identityManager = identityManager ??
                throw new ArgumentNullException(nameof(identityManager));
        }

        /// <inheritdoc />
        public async Task<int?> GetCompanyIdByPermission(string token, string[] scopes, int? companyId = null)
        {
            string role = "turututu";
            var userInfo = await _httpService.GetCurrentUser(token);
            if (!scopes.All(x => userInfo.Scopes.Contains(x)))
            {
                return userInfo.CompanyId ?? throw new ArgumentNullException("CompanyId of user info" + Environment.NewLine + "token: " + token + Environment.NewLine + "scopes:" + string.Join($",{Environment.NewLine}", scopes) + "role: " + role);
            }
            return companyId;
        }

        /// <inheritdoc />
        public async Task ValidateCompanyAndBranch(int companyId, Guid branchId, string token)
        {
            var company = await _httpService.GetCompanyById(companyId, token);
            if (company == null)
            {
                throw new ApplicationException($"Company with id: {companyId.ToString()} does not exist");
            }
            var branch = await _httpService.GetBranchById(branchId, token);
            if (branch == null)
            {
                throw new ApplicationException($"Branch with id: {branchId.ToString()} does not exist");
            }
            if (branch.CompanyId != company.Id)
            {
                throw new ApplicationException($"Branch with id: {branchId.ToString()} has not company with id: {companyId.ToString()}");
            }
        }
    }
}
