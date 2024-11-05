using Backoffice.API.ActionFilter;
using Backoffice.API.Repositories;
using Backoffice.API.Telemetry;
using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Backoffice.API
{
    //
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //
            // Telemetry
            Action<ResourceBuilder> configureResource = r => r.AddService(
                serviceName: "Backoffice.API",
                serviceNamespace: Configuration["Telemetry:Environment"],
                serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "latest",
                serviceInstanceId: Environment.MachineName);

            services.AddSingleton<Instrumentation>();

            if (Convert.ToBoolean(Configuration["Telemetry:Enabled"]))
            {

                services
                  .AddOpenTelemetry()
                  .ConfigureResource(configureResource)
                  .WithTracing(tracing =>
                  {
                      tracing
                      .AddSource(Instrumentation.ActivitySourceName)
                      .SetSampler(new AlwaysOnSampler())
                      .AddHttpClientInstrumentation(
                        (opts) => opts.FilterHttpRequestMessage =
                          (req) =>
                          {
                              var uriString = req.RequestUri.ToString();
                              var isAzureReq = uriString.Contains("azure.com") ||
                                               uriString.Contains("visualstudio.com");
                              return !isAzureReq;
                          }
                      )
                      .AddAspNetCoreInstrumentation((aspCoreOpts) => aspCoreOpts.Filter = httpContext =>
                      {
                        var path = httpContext.Request.Path.ToString();
                        var toExclude = path.StartsWith("/api") || path.StartsWith("/sportsbook");
                        return !toExclude;
                      })
                      .AddSqlClientInstrumentation();

                      services.Configure<AspNetCoreTraceInstrumentationOptions>(Configuration.GetSection("Telemetry:AspNetCoreInstrumentation"));

                      tracing.AddOtlpExporter(exporterOpts =>
                    {
                        exporterOpts.Protocol = OtlpExportProtocol.HttpProtobuf;
                        exporterOpts.Endpoint = new Uri(Configuration["Telemetry:Collector"] + "/v1/traces");
                    });

                  })
                  .WithMetrics(metrics =>
                  {
                      metrics
                      .AddMeter(Instrumentation.MeterName)
                      .AddRuntimeInstrumentation()
                      .AddHttpClientInstrumentation()
                      .AddAspNetCoreInstrumentation();

                      metrics.AddOtlpExporter(exporterOpts =>
                    {
                        exporterOpts.Protocol = OtlpExportProtocol.HttpProtobuf;
                        exporterOpts.Endpoint = new Uri(Configuration["Telemetry:Collector"] + "/v1/metrics");
                    });

                  });

            } //if telemetry enabled

            services.AddControllers();
            services.AddCors(options =>
            {
                options.AddPolicy("AllowOrigin", options => options.AllowAnyOrigin());
                options.AddPolicy("AllowAllOrigins",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
                    });
            });
            services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("v1", new OpenApiInfo { Title = "Backoffice API", Version = "v1" });
                option.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    Name = "x-api-key",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Description = "Authorization by x-api-key inside request's header",
                    Scheme = "ApiKeyScheme"
                });
                var key = new OpenApiSecurityScheme()
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "ApiKey"
                    },
                    In = ParameterLocation.Header
                };

                var requirement = new OpenApiSecurityRequirement { { key, new List<string>() } };
                option.AddSecurityRequirement(requirement);
            });
            services.AddControllers().AddJsonOptions(opt =>
            {
                opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
            services.AddHttpClient<IAuthenticationRepository, AuthenticationRepository>(client =>
            {
                client.BaseAddress = new Uri(Configuration["API:AuthenticationAPI:Uri"]);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-api-key", Configuration["API:AuthenticationAPI:Security:APIKey"]);
            });
            services.AddDbContextPool<BackofficeContext>(o => o.UseSqlServer(Configuration["ConnectionStrings:BackOffice"], options =>
                options.CommandTimeout(180))); //180 seconds timeout
            services.AddHttpClient<IRabbitMqRepository, RabbitMqRepository>();
            services.AddScoped<IBackofficeRepository, BackofficeRepository>();
            services.AddHttpClient<IMaintenanceRepository, MaintenanceRepository>(client =>
            {
                client.BaseAddress = new Uri(Configuration["API:Maintenance:Uri"]);
                client.DefaultRequestHeaders.Add("x-api-key", "API:Maintenance:Security:ApiKey");
            });
            services.AddScoped<IPlatformRepository, PlatformRepository>();
            services.AddScoped<ITradingRepository, TradingRepository>();
            services.AddScoped<IBackofficeActivityLogRepository, BackofficeActivityLogRepository>();
            services.AddSingleton<IMemCache, MemCache>();
            services.AddScoped<ApiKeyFilter>();
            services.AddControllers().AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);
            services.AddHealthChecks();
            services.AddApplicationInsightsTelemetry();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseRouting();
            app.UseCors();
            app.UseAuthorization();

            //app.UseHttpsRedirection();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/healthz");
            });
        }
    }
}
