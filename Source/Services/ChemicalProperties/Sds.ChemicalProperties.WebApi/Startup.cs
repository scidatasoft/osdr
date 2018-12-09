using MassTransit;
using MassTransit.RabbitMqTransport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.GridFs;
using Serilog;
using System;
using System.Linq;
using System.Net;
using System.Reflection;

namespace Sds.ChemicalProperties.WebApi
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(context => Bus.Factory.CreateUsingRabbitMq(x =>
            {
                IRabbitMqHost host = x.Host(new Uri(Environment.ExpandEnvironmentVariables(Configuration["RabbitMq:ConnectionString"])), h => { });

                x.UseSerilog();
            }));

            //Register MongoDB 
            var connectionString = Environment.ExpandEnvironmentVariables(Configuration["OsdrConnectionSettings:ConnectionString"]);
            services.AddSingleton(new MongoClient(connectionString));
            services.AddScoped(service => service.GetService<MongoClient>().GetDatabase(Configuration["OsdrConnectionSettings:DatabaseName"]));

            services.AddTransient<IBlobStorage, GridFsStorage>(
               x => new GridFsStorage(connectionString, Configuration["OsdrConnectionSettings:DatabaseName"]));

            services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            loggerFactory.AddSerilog();

            app.UseIdentityServerAuthentication(new IdentityServerAuthenticationOptions
            {
                Authority = Environment.ExpandEnvironmentVariables(Configuration["IdentityServer:Authority"]),
                AllowedScopes = new[] { "osdr-api" },
                RequireHttpsMetadata = false,
                ApiName = "osdr-api",
                // this is only needed because IS3 does not include the API name in the JWT audience list
                // so we disable UseIdentityServerAuthentication JWT audience check and rely upon
                // scope validation to ensure we're only accepting tokens for the right API
                LegacyAudienceValidation = true,
            });

            app.UseExceptionHandler(options =>
            {
                options.Run(async context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await context.Response.WriteAsync("Internal server error. Try again letter");
                });
            });

            app.UseCors(
                builder => builder.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());

            app.UseMvc();

            var busControl = app.ApplicationServices.GetService<IBusControl>();

            busControl.Start();
        }
    }
}
