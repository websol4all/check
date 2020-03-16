using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

using AutoFixture;
using AutoFixture.Kernel;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using StackExchange.Redis;

using Xunit;

using Xyzies.Devices.Data;
using Xyzies.Devices.Services.Common.Cache;
using Xyzies.Devices.Services.Helpers.Options;
using Xyzies.Devices.Services.Models.Branch;
using Xyzies.Devices.Services.Models.Company;
using Xyzies.Devices.Tests.IntegrationTests.Services;
using Xyzies.Devices.Tests.Models.User;

namespace Xyzies.Devices.Tests
{
    public class BaseTest : IAsyncLifetime
    {
        private readonly object _lock = new object();

        public TestServer TestServer;
        public DeviceContext DbContext;

        public Fixture Fixture;

        public IMemoryCache memoryCache = null;
        public RedisStore redisStore;
        public IOptionsMonitor<NotificationSenderExtentionOptions> _notificationSenderExtentionOptions;

        public virtual async Task InitializeAsync()
        {
            lock(_lock)
            {
                var builder = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional : true);

                var configuration = builder.Build();

                IWebHostBuilder webHostBuild =
                    WebHost.CreateDefaultBuilder()
                    .UseStartup<TestStartUp>()
                    .UseWebRoot(Directory.GetCurrentDirectory())
                    .UseContentRoot(Directory.GetCurrentDirectory());

                var dbConnectionString = configuration.GetConnectionString("db");

                if (string.IsNullOrEmpty(dbConnectionString))
                {
                    throw new ApplicationException("Missing the connection string to database");
                };
                webHostBuild.ConfigureServices(service =>
                {
                    service.AddDbContextPool<DeviceContext>(ctxOptions => ctxOptions.UseInMemoryDatabase(dbConnectionString).EnableSensitiveDataLogging());
                    service.Configure<UserLoginOption>(options => configuration.Bind("TestUserCredential", options));
                    service.AddScoped<IHttpServiceTest, HttpServiceTest>();
                    service.AddMemoryCache();
                    service.Configure<NotificationSenderExtentionOptions>(options => configuration.Bind("NotificationHelper", options));

                    RedisConnection.InitializeConnectionString(configuration.GetSection("ConnectionStrings")["redis"]);
                    service.AddSingleton<RedisStore>();
                });
                TestServer = new TestServer(webHostBuild);
                DbContext = TestServer.Host.Services.GetRequiredService<DeviceContext>();
                Fixture = new Fixture();
                Fixture.Customizations.Add(new IgnoreVirtualMembers());
                memoryCache = TestServer.Host.Services.GetRequiredService<IMemoryCache>();

                _notificationSenderExtentionOptions = TestServer.Host.Services.GetRequiredService<IOptionsMonitor<NotificationSenderExtentionOptions>>();
                redisStore = TestServer.Host.Services.GetRequiredService<RedisStore>();
            }
        }

        public virtual async Task DisposeAsync()
        {
            DbContext.Dispose();
            TestServer.Dispose();
        }
    }

    public class IgnoreVirtualMembers : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            var pi = request as PropertyInfo;
            if (pi == null)
            {
                return new NoSpecimen();
            }

            if (pi.GetGetMethod().IsVirtual)
            {
                return null;
            }
            return new NoSpecimen();
        }
    }
}
