using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xyzies.Devices.Services.Helpers;
using Xyzies.Devices.Services.Models.Branch;
using Xyzies.Devices.Services.Models.Company;
using Xyzies.Devices.Services.Models.User;
using Xyzies.Devices.Tests.Models.Branch;
using Xyzies.Devices.Tests.Models.Company;
using Xyzies.Devices.Tests.Models.User;

namespace Xyzies.Devices.Tests.IntegrationTests.Services
{
    public class HttpServiceTest : IHttpServiceTest
    {
        private readonly string _identityServiceUsrl = null;
        private readonly string _publicApiServiceUrl = null;

        private readonly ILogger<HttpServiceTest> _logger = null;

        public HttpServiceTest(IOptions<ServiceOption> options, ILogger<HttpServiceTest> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _identityServiceUsrl = options?.Value?.IdentityServiceUrl ?? throw new InvalidOperationException("Missing identity service url");
            _publicApiServiceUrl = options?.Value?.PublicApiUrl ?? throw new InvalidOperationException("Missing public-api service url");
        }

        public async Task<TokenModel> GetAuthorizationToken(UserLoginOption userLogin)
        {
            var uri = new Uri($"{_identityServiceUsrl}/authorize/token");
            var responseString = await SendRequestAsync(uri, HttpMethod.Post, JsonConvert.SerializeObject(userLogin));

            return DeserializeResultFromResponseString<TokenModel>(responseString);
        }

        public async Task<UserModelTest> CreateNewTestUser(CreateUserModel user, TokenModel token)
        {
            var uri = new Uri($"{_identityServiceUsrl}/users");
            var responseString = await SendRequestAsync(uri, HttpMethod.Post, JsonConvert.SerializeObject(user), token);

            return DeserializeResultFromResponseString<UserModelTest>(responseString);
        }

        public async Task DeleteTestUser(Guid userId, TokenModel token)
        {
            var uri = new Uri($"{_identityServiceUsrl}/users/{userId.ToString()}");
            var responseString = await SendRequestAsync(uri, HttpMethod.Delete, token: token);
        }

        public async Task<int> CreateNewTestCompany(CreateCompanyModel company, TokenModel token)
        {
            var uri = new Uri($"{_publicApiServiceUrl}/company");
            var responseString = await SendRequestAsync(uri, HttpMethod.Post, JsonConvert.SerializeObject(company), token);

            return DeserializeResultFromResponseString<int>(responseString);
        }

        public async Task<CompanyModel> GetTestCompany(string companyName, TokenModel token, int? id = null)
        {
            Uri uri = null;
            if (id.HasValue)
            {
                uri = new Uri($"{_publicApiServiceUrl}/company/{ConstTest.StaticToken}/trusted/internal?Id={id.Value.ToString()}");
            }
            else
            {
                uri = new Uri($"{_publicApiServiceUrl}/company/{ConstTest.StaticToken}/trusted/internal?CompanyName={companyName}");
            }
            var responseString = await SendRequestAsync(uri, HttpMethod.Get, token: token);
            return DeserializeResultFromResponseString<CompanyModel>(responseString);
        }

        public async Task<Guid> CreateNewTestBranch(CreateBranchModel branch, TokenModel token)
        {
            var uri = new Uri($"{_publicApiServiceUrl}/branch");
            var responseString = await SendRequestAsync(uri, HttpMethod.Post, JsonConvert.SerializeObject(branch), token);

            return DeserializeResultFromResponseString<Guid>(responseString);
        }

        public async Task<BranchModel> GetTestBranch(string branchName, TokenModel token, Guid? id = null)
        {
            Uri uri = null;
            if (id.HasValue)
            {
                uri = new Uri($"{_publicApiServiceUrl}/branch/{ConstTest.StaticToken}/trusted/internal?Id={id.Value.ToString()}");
            }
            else
            {
                uri = new Uri($"{_publicApiServiceUrl}/branch/{ConstTest.StaticToken}/trusted/internal?BranchName={branchName}");
            }
            var responseString = await SendRequestAsync(uri, HttpMethod.Get, token: token);
            return DeserializeResultFromResponseString<BranchModel>(responseString);
        }

        private async Task<string> SendRequestAsync(Uri uri, HttpMethod method, string body = null, TokenModel token = null)
        {
            using (HttpClient client = new HttpClient())
            {

                if (token != null && !string.IsNullOrWhiteSpace(token.TokenType) && !string.IsNullOrWhiteSpace(token.AccessToken))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);
                }

                HttpRequestMessage requestMessage = new HttpRequestMessage(method, uri);

                if (!string.IsNullOrWhiteSpace(body))
                {
                    requestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }

                var response = await client.SendAsync(requestMessage);
               
                _logger.LogInformation($"[SendRequestAsync] request = {body}{Environment.NewLine}responseCode = {response.StatusCode}; responseMessage = {await response.Content.ReadAsStringAsync()}");

                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();

                return responseString;
            }
        }

        private T DeserializeResultFromResponseString<T>(string responseString)
        {
            if (string.IsNullOrWhiteSpace(responseString))
            {
                return default(T);
            }
            return JsonConvert.DeserializeObject<T>(responseString);
        }
    }
}
