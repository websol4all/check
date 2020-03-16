using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xyzies.Devices.Services.Models.Branch;
using Xyzies.Devices.Services.Models.Company;
using Xyzies.Devices.Services.Models.User;
using Xyzies.Devices.Tests.Models.Branch;
using Xyzies.Devices.Tests.Models.Company;
using Xyzies.Devices.Tests.Models.User;

namespace Xyzies.Devices.Tests.IntegrationTests.Services
{
    public interface IHttpServiceTest
    {
        Task<TokenModel> GetAuthorizationToken(UserLoginOption userLogin);
        Task<UserModelTest> CreateNewTestUser(CreateUserModel user, TokenModel token);
        Task DeleteTestUser(Guid userId, TokenModel token);
        Task<int> CreateNewTestCompany(CreateCompanyModel company, TokenModel token);
        Task<CompanyModel> GetTestCompany(string companyName, TokenModel token, int? id = null);
        Task<Guid> CreateNewTestBranch(CreateBranchModel branch, TokenModel token);
        Task<BranchModel> GetTestBranch(string branchName, TokenModel token, Guid? id = null);
    }
}
