using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Ardas.AspNetCore.Logging;

using IdentityServiceClient;
using IdentityServiceClient.Middlewares;

using Mapster;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using StackExchange.Redis;

using Swashbuckle.AspNetCore.Swagger;

using Xyzies.Devices.API.Options;
using Xyzies.Devices.Data;
using Xyzies.Devices.Data.Repository;
using Xyzies.Devices.Data.Repository.Behaviour;
using Xyzies.Devices.Services.Common.Cache;
using Xyzies.Devices.Services.Helpers;
using Xyzies.Devices.Services.Helpers.Interfaces;
using Xyzies.Devices.Services.Helpers.Options;
using Xyzies.Devices.Services.Mappers;
using Xyzies.Devices.Services.Service;
using Xyzies.Devices.Services.Service.BackGroundWorkerService;
using Xyzies.Devices.Services.Service.Interfaces;

namespace Xyzies.Devices.API
{
    public class Startup
    {
        private readonly ILogger _logger = null;
        private readonly string _deviceEnvironmentName = "DeviceEnvironment";
        private readonly string _deviceEnvironment = null;
        private readonly string _deviceTestEnvironment = "test";

        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            Configuration = configuration;
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _deviceEnvironment = Environment.GetEnvironmentVariable(_deviceEnvironmentName);
        }

        public IConfiguration Configuration { get; set; }

        public static string ServiceBaseUrlPrefix { get; set; } = "/api/device-management-api"; // Default.

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(AzureADB2CDefaults.BearerAuthenticationScheme)
                .AddAzureADB2CBearer(options => Configuration.Bind("AzureAdB2C", options));
            services.AddCors();
            services.AddDataProtection();
            services.AddHealthChecks();
            services.AddHttpContextAccessor();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.Configure<AssemblyOptions>(options => Configuration.Bind("AssemblyVersion", options));

            services.Configure<ServiceOption>(option => Configuration.Bind("Services", option));

            services.Configure<NotificationSenderExtentionOptions>(options => Configuration.Bind("NotificationHelper", options));

            services.AddIdentityClient(options =>
            {
                options.ServiceUrl = options.ServiceUrl = Configuration.GetSection("Services")["IdentityServiceUrl"];
            });

            services.AddSwaggerGen(options =>
            {
                options.SwaggerGeneratorOptions.IgnoreObsoleteActions = true;

                options.SwaggerDoc("v1", new Info
                {
                    Title = "Xyzies.Devices",
                        Version = $"v1.0.0",
                        Description = ""
                });

                options.AddSecurityDefinition("Bearer", new ApiKeyScheme
                {
                    In = "header",
                        Name = "Authorization",
                        Description = "Please enter JWT with Bearer into field",
                        Type = "apiKey"
                });

                options.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                { { "Bearer", Enumerable.Empty<string>() }
                });

                options.CustomSchemaIds(x => x.FullName);
                options.EnableAnnotations();
                options.DescribeAllEnumsAsStrings();
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory,
                    string.Concat(Assembly.GetExecutingAssembly().GetName().Name, ".xml")));
            });

            services.AddDbContext<DeviceContext>(ctxOptions =>
                ctxOptions.UseSqlServer(Configuration.GetConnectionString("db")));

            services.AddSignalR(hubOptions =>
            {
                hubOptions.EnableDetailedErrors = true;
            }).AddStackExchangeRedis(Configuration.GetConnectionString("redis"), options =>
            {
                options.Configuration.ChannelPrefix = "SignalRDeviceManagment";
            });

            services.AddMemoryCache();

            #region DI settings

            services.AddScoped<IHttpService, HttpService>();
            services.AddScoped<IDeviceRepository, DeviceRepository>();
            services.AddScoped<IDeviceService, DeviceService>();
            services.AddScoped<IValidationHelper, ValidationHelper>();
            services.AddScoped<IDeviceHistoryRepository, DeviceHistoryRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<IDeviceHistoryService, DeviceHistoryService>();
            services.AddScoped<ILogRepository, LogRepository>();
            services.AddScoped<INotificationSender, NotificationSender>();

            RedisConnection.InitializeConnectionString(Configuration.GetSection("ConnectionStrings")["redis"]);
            services.AddSingleton<RedisStore>();

            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddSingleton<BadDisconnectSocketService>();

            #endregion

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };

            _logger.LogInformation($"[Startup.ConfigureServices] dispute environment: {_deviceEnvironment}");
            if (_deviceEnvironment?.ToLower() != _deviceTestEnvironment.ToLower())
            {
                services.AddTcpStreamLogging(options => Configuration.Bind("Logstash", options));
            }
            TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true);
            MapperConfigure.Configure();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            using(var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetService<DeviceContext>();
                context.Database.EnsureCreated();
                _logger.LogInformation($"[Startup.ConfigureServices] dispute environment: {_deviceEnvironment}");
                if (_deviceEnvironment?.ToLower() != _deviceTestEnvironment.ToLower())
                {
                    context.Database.Migrate();
                }
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days.. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseCors("dev")
                .UseAuthentication()
                .UseClientMiddleware()
                .UseMvc()
                .UseHealthChecks("/api/health")
                .UseSwagger(options =>
                {
                    options.PreSerializeFilters.Add((swaggerDoc, httpReq) => swaggerDoc.BasePath = $"{ServiceBaseUrlPrefix}");

                    options.RouteTemplate = "/swagger/{documentName}/swagger.json";

                })
                .UseSwaggerUI(uiOptions =>
                {
                    uiOptions.SwaggerEndpoint("v1/swagger.json", $"v1.0.0");
                    uiOptions.DisplayRequestDuration();
                });

            app.UseSignalR(routes =>
            {
                routes.MapHub<DeviceHubService>("/deviceSocket");
                routes.MapHub<WebHubService>("/operatorSocket"); //
            });

            _logger.LogDebug("Startup configured successfully."); //
        }
    }
}
