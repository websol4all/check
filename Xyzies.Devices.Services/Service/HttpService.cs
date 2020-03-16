using IdentityServiceClient;
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
using Xyzies.Devices.Services.Models.Tenant;
using Xyzies.Devices.Services.Models.User;
using Xyzies.Devices.Services.Service.Interfaces;

namespace Xyzies.Devices.Services.Service
{
    /// <inheritdoc />
    public class HttpService : IHttpService
    {
        private readonly string _publicApiUrl = null;
        private readonly string _identityServiceUrl = null;
        private readonly string _notificationServiceUrl = null;
        private readonly string _publicApiObjectData = "data";
        private readonly string _identityObjectData = "result";
        public const string StaticToken = "d64ded6fd8db3fead6c90e600d85cccc02cd3e2dafcc29e8d1ade61263229d0b16b5a92ffa1bad1d6325e302461c7a69630c5c913ab47fb7e284dcabba1ac91e";
        private readonly ILogger<HttpService> _logger = null;
        /// <summary>
        /// Http service
        /// </summary>
        /// <param name="options"></param>
        public HttpService(IOptionsMonitor<ServiceOption> options, ILogger<HttpService> logger)
        {
            _publicApiUrl = options.CurrentValue?.PublicApiUrl ??
                throw new ArgumentNullException(nameof(options), "Missing URL to public-api");
            _identityServiceUrl = options.CurrentValue?.IdentityServiceUrl ??
                throw new ArgumentNullException(nameof(options), "Missing URL to identity service");
            _notificationServiceUrl = options.CurrentValue?.NotificationServiceUrl ??
                throw new ArgumentNullException(nameof(options), "Missing URL to notification service");
            _logger = logger ??
               throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<CompanyModel> GetCompanyById(int id, string token)
        {
            var uri = new Uri($"{_publicApiUrl}/company/{id}");
            var responseString = await SendGetRequest(uri, token);

            return GetApiResponse<CompanyModel>(responseString, _publicApiObjectData);
        }

        /// <inheritdoc />
        public async Task<BranchModel> GetBranchById(Guid id, string token)
        {
            var uri = new Uri($"{_publicApiUrl}/branch/{id.ToString()}");
            var responseString = await SendGetRequest(uri, token);

            return GetApiResponse<BranchModel>(responseString, _publicApiObjectData);
        }

        /// <inheritdoc />
        public async Task<UserModel> GetCurrentUser(string token)
        {
            var uri = new Uri($"{_identityServiceUrl}/users/profile");
            var responseString = await SendGetRequest(uri, token);

            return GetApiResponse<UserModel>(responseString, _identityObjectData);
        }

        /// <inheritdoc />
        public async Task<List<UserModel>> GetUsersByIdsAsync(string token, string idsQeury)
        {
            var uri = new Uri($"{_identityServiceUrl}/users?{idsQeury}");
            var responseString = await SendGetRequest(uri, token);

            return GetApiResponse<List<UserModel>>(responseString, _identityObjectData);
        }

        /// <inheritdoc />
        public async Task<List<CompanyModel>> GetCompaniesByIds(string idsQeury, string token)
        {
            var uri = new Uri($"{_publicApiUrl}/company?{idsQeury}");
            var responseString = await SendGetRequest(uri, token);

            return GetApiResponse<List<CompanyModel>>(responseString, _publicApiObjectData);
        }

        /// <inheritdoc />
        public async Task<TenantFullModel> GetTenantSingleByCompanyId(int companyId, string token)
        {
            var uri = new Uri($"{_publicApiUrl}/tenant/single/{companyId}/by-company");
            var responseString = await SendGetRequest(uri, token);

            return GetApiResponse<TenantFullModel>(responseString, _publicApiObjectData);
        }

        /// <inheritdoc />
        public async Task<List<TenantFullModel>> GetTenantsByIds(string token, string idsQeury = null)
        {
            var uri = new Uri($"{_publicApiUrl}/tenant?{idsQeury}");
            var responseString = await SendGetRequest(uri, token);

            return GetApiResponse<List<TenantFullModel>>(responseString, _publicApiObjectData);
        }

        /// <inheritdoc />
        public async Task<List<BranchModel>> GetBranchesByIds(string idsQeury, string token)
        {
            var uri = new Uri($"{_publicApiUrl}/branch?{idsQeury}");
            var responseString = await SendGetRequest(uri, token);

            return GetApiResponse<List<BranchModel>>(responseString, _publicApiObjectData);
        }

        /// <inheritdoc />
        public async Task<List<UserModel>> GetUsersByIdTrustedAsync(string query)
        {
            var uri = new Uri($"{_identityServiceUrl}/users/{StaticToken}/trusted/filtered");

            var responseString = await SendRequestAsync(uri, HttpMethod.Post, query, null);

            return GetApiResponse<List<UserModel>>(responseString, _identityObjectData);
        }

        public async Task<List<BranchModel>> GetBranchesTrustedAsync()
        {
            var uri = new Uri($"{_publicApiUrl}/branch/{StaticToken}/trusted");

            var responseString = await SendRequestAsync(uri, HttpMethod.Get, null, null);

            return GetApiResponse<List<BranchModel>>(responseString, _publicApiObjectData);
        }

        public async Task PostNotificationEmailAsync(string query)
        {
            var uri = new Uri($"{_notificationServiceUrl}/sendemail/{StaticToken}/trusted");

            await SendRequestAsync(uri, HttpMethod.Post, query, null);
        }

        public async Task<List<CompanyModel>> GetCompaniesForTrustedAsync()
        {
            var uri = new Uri($"{_publicApiUrl}/company/{StaticToken}/trusted");

            var responseString = await SendRequestAsync(uri, HttpMethod.Get, null, null);

            return GetApiResponse<List<CompanyModel>>(responseString, _publicApiObjectData);
        }

        private T GetApiResponse<T>(string responseString, string sectionName)
        {
            if (string.IsNullOrWhiteSpace(responseString))
            {
                return default(T);
            }
            var data = JToken.Parse(responseString);
            if ((data as JObject) != null && data[sectionName] != null)
            {
                return data[sectionName].ToObject<T>();
            }
            return JsonConvert.DeserializeObject<T>(responseString);
        }

        //TODO: we can use instead sendGetRequest method SendRequestAsync like generic
        private async Task<string> SendGetRequest(Uri uri, string token = null)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = uri;
                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await client.GetAsync(client.BaseAddress);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                var responseString = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new ApplicationException(responseString);
                }

                return responseString;
            }
        }

        private async Task<string> SendRequestAsync(Uri uri, HttpMethod method, string body, string token)
        {
            using (HttpClient client = new HttpClient())
            {

                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                HttpRequestMessage requestMessage = new HttpRequestMessage(method, uri);

                if (!string.IsNullOrWhiteSpace(body))
                {
                    requestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }

                var response = await client.SendAsync(requestMessage);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                var responseString = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"SendRequestAsync: response.StatusCode: {response.StatusCode}, message: {responseString}", response.StatusCode, responseString);
                    throw new ApplicationException(responseString);
                }

                return responseString;
            }
        }
    }
}
