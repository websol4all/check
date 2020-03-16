using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xyzies.Devices.Services.Models.Branch;
using Xyzies.Devices.Services.Models.Company;
using Xyzies.Devices.Tests.IntegrationTests.Services;
using Xyzies.Devices.Tests.Models.User;
using AutoFixture;
using System.Linq;

namespace Xyzies.Devices.Tests
{
    public class BaseIntegrationTest : BaseTest
    {
        private readonly string CablePortalRequestStatusApprovedId;


        public HttpClient HttpClient;
        public UserModelTest AccountAdmin;
        public UserModelTest OperationAdmin;
        public UserModelTest SystemAdmin;
        public UserModelTest Supervisor;
        public UserModelTest SupervisorWithCompanyWithoutBranch;
        public UserModelTest Manager;
        public IHttpServiceTest HttpServiceTest = null;
        public Dictionary<string, UserModelTest> Users;
        public CompanyModel Company = null;
        public CompanyModel CompanyWithoutAnyBranch = null;
        public BranchModel Branch = null;

        private TokenModel _adminToken;
        private UserLoginOption _userLogin = null;

        public BaseIntegrationTest()
        {
            CablePortalRequestStatusApprovedId = "a20d6ff1-2b74-4883-b982-407c686b796b";
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            HttpClient = TestServer.CreateClient();
            _userLogin = TestServer.Host.Services.GetRequiredService<IOptions<UserLoginOption>>()?.Value;
            HttpServiceTest = TestServer.Host.Services.GetRequiredService<IHttpServiceTest>();
            _adminToken = await HttpServiceTest.GetAuthorizationToken(_userLogin);
            AccountAdmin = await HttpServiceTest.CreateNewTestUser(FormCreateUserModel(ConstTest.Role.AccountAdmin), _adminToken);
            OperationAdmin = await HttpServiceTest.CreateNewTestUser(FormCreateUserModel(ConstTest.Role.OperationAdmin), _adminToken);
            SystemAdmin = await HttpServiceTest.CreateNewTestUser(FormCreateUserModel(ConstTest.Role.SystemAdmin), _adminToken);
            Manager = await HttpServiceTest.CreateNewTestUser(FormCreateUserModel(ConstTest.Role.Manager), _adminToken);

            Company = await HttpServiceTest.GetTestCompany(ConstTest.DefaultCompanyName, _adminToken);
            CompanyWithoutAnyBranch = await HttpServiceTest.GetTestCompany(ConstTest.DefaultCompanyNameNotBindAnyBranch, _adminToken);

            Branch = await HttpServiceTest.GetTestBranch(ConstTest.DefaultBranchName, _adminToken);

            Supervisor = await HttpServiceTest.CreateNewTestUser(FormCreateUserModel(ConstTest.Role.Supervisor, Company.Id, Branch.Id), _adminToken);
            SupervisorWithCompanyWithoutBranch = await HttpServiceTest.CreateNewTestUser(FormCreateUserModel(ConstTest.Role.Supervisor, CompanyWithoutAnyBranch.Id), _adminToken);
            Users = FillingUser();
        }

        public CreateUserModel FormCreateUserModel(string roleName, int? companyId = null, Guid? branchId = null)
        {
            string email = $"{Fixture.Create<string>()}@test.com";
            var passwordProfile = Fixture.Build<PasswordProfileModel>()
                                         .With(x => x.Password, ConstTest.Password)
                                         .With(x => x.ForceChangePasswordNextLogin, false)
                                         .Create();
            var signInNames = Fixture.Build<SignInName>()
                                     .With(x => x.Value, email)
                                     .With(x => x.Type, ConstTest.DefaultSignInNamesTypeForEmail)
                                     .CreateMany(1)
                                     .ToList();
            var createUserModel = Fixture.Build<CreateUserModel>()
                                         .With(x => x.Role, roleName)
                                         .With(x => x.SignInNames, signInNames)
                                         .With(x => x.PasswordProfile, passwordProfile)
                                         .With(x => x.CompanyId, companyId)
                                         .With(x => x.BranchId, branchId)
                                         .With(x => x.CreationType, "LocalAccount")
                                         .With(x => x.AccountEnabled, true)
                                         .With(x => x.DisplayName, email)
                                         .With(x => x.GivenName, email)
                                         .With(x => x.Surname, email)
                                         .With(x => x.StatusId, Guid.Parse(CablePortalRequestStatusApprovedId))
                                         .Create();
            return createUserModel;
        }

        private Dictionary<string, UserModelTest> FillingUser()
        {
            return new Dictionary<string, UserModelTest>()
           {
                { ConstTest.Role.AccountAdmin, AccountAdmin },
                { ConstTest.Role.OperationAdmin, OperationAdmin},
                { ConstTest.Role.SystemAdmin, SystemAdmin },
                { ConstTest.Role.Supervisor, Supervisor}
           };
        }
        public override async Task DisposeAsync()
        {
            await HttpServiceTest.DeleteTestUser(AccountAdmin.Id, _adminToken);
            await HttpServiceTest.DeleteTestUser(OperationAdmin.Id, _adminToken);
            await HttpServiceTest.DeleteTestUser(SystemAdmin.Id, _adminToken);
            await HttpServiceTest.DeleteTestUser(Supervisor.Id, _adminToken);
            await HttpServiceTest.DeleteTestUser(SupervisorWithCompanyWithoutBranch.Id, _adminToken);
            await HttpServiceTest.DeleteTestUser(Manager.Id, _adminToken);
            HttpClient.Dispose();
            await base.DisposeAsync();
        }
    }
}
