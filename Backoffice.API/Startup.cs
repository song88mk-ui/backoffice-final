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
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Backoffice.API
{
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
            services.AddSingleton<Instrumentation>();
            services.AddTelemetry(new TelemetrySettings(
                ServiceName: Instrumentation.ActivitySourceName,
                ActivitySourceName: Instrumentation.ActivitySourceName,
                MeterName: Instrumentation.MeterName));

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

            var dbContextTimeout = Configuration.GetValue<int>("Custom:DbContextTimeout", 180);
            services.AddDbContextPool<BackofficeContext>(o => o.UseSqlServer(Configuration["ConnectionStrings:BackOffice"], options =>
                options.CommandTimeout(dbContextTimeout)));

            services.AddHttpClient<IRabbitMqRepository, RabbitMqRepository>();
            services.AddScoped<IBackofficeRepository, BackofficeRepository>();
            services.AddHttpClient<IMaintenanceRepository, MaintenanceRepository>(client =>
            {
                client.BaseAddress = new Uri(Configuration["API:Maintenance:Uri"]);
                client.DefaultRequestHeaders.Add("x-api-key", "API:Maintenance:Security:ApiKey");
            });

            services.AddScoped<IPlatformRepository, PlatformRepository>();
            services.AddScoped<ISportsbookRepository, SportsbookRepository>();
            services.AddScoped<ITradingRepository, TradingRepository>();
            services.AddScoped<IBackofficeActivityLogRepository, BackofficeActivityLogRepository>();
            services.AddSingleton<IMemCache, MemCache>();
            services.AddTransient<ApiKeyFilter>();
            services.AddControllers().AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);
            services.AddHealthChecks();
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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/healthz");
            });
        }
    }
}
