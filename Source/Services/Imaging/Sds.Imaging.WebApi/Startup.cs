using Collector.Serilog.Enrichers.Assembly;
using IdentityModel.AspNetCore.OAuth2Introspection;
using IdentityServer4.AccessTokenValidation;
using MassTransit;
using MassTransit.RabbitMqTransport;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Sds.Imaging.WebApi.Swagger;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.GridFs;
using Serilog;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Sds.Imaging.WebApi
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
                .Enrich.With<SourceSystemInformationalVersionEnricher<Controllers.VersionController>>()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConfiguration>(Configuration);

            services.AddSingleton(context => Bus.Factory.CreateUsingRabbitMq(x =>
            {
                IRabbitMqHost host = x.Host(new Uri(Environment.ExpandEnvironmentVariables(Configuration["RabbitMq:ConnectionString"])), h => { });

                x.UseSerilog();
            }));

            //Register MongoDB and GridFS 
            var connectionString = Environment.ExpandEnvironmentVariables(Configuration["OsdrConnectionSettings:ConnectionString"]);
            services.AddTransient(x => new MongoClient(connectionString).GetDatabase(Configuration["OsdrConnectionSettings:DatabaseName"]));
            services.AddTransient<IBlobStorage, GridFsStorage>(x => new GridFsStorage(x.GetService<IMongoDatabase>()));

            services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Imaging API", Version = "v1" });

                //Set the comments path for the swagger json and ui.
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                var xmlPath = Path.Combine(basePath, "Sds.Imaging.WebApi.xml");
                c.IncludeXmlComments(xmlPath);
                c.DocumentFilter<LowercaseDocumentFilter>();
                c.OperationFilter<SecurityRequirementsOperationFilter>(); // Adds an Authorization input box to every endpoint
                c.OperationFilter<AddUploadFileParameter>();

            });

            var authorityUrl = Environment.ExpandEnvironmentVariables(Configuration["IdentityServer:Authority"]);
            Log.Information($"Identity server: {authorityUrl}");

            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(cfg =>
            {
                cfg.Authority = authorityUrl;
                cfg.IncludeErrorDetails = true;
                cfg.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidIssuer = authorityUrl,
                    ValidateLifetime = true
                };

                cfg.Events = new JwtBearerEvents()
                {
                    OnAuthenticationFailed = c =>
                    {
                        c.NoResult();
                        c.Response.StatusCode = 401;
                        c.Response.ContentType = "text/plain";
                        return c.Response.WriteAsync(c.Exception.ToString());
                    },

                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        // If the request is for our hub...
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            // Read the token out of the query string
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // Add framework services.
            services.AddMvc()
                .AddJsonOptions(opt => opt.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            loggerFactory.AddSerilog();

            app.UseStaticFiles();
            app.UseAuthentication();
                        
            app.UseExceptionHandler(options =>
            {
                options.Run(async context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await context.Response.WriteAsync("Internal server error. Try again letter");
                });
            });

            // Enable middle-ware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger(c => c.PreSerializeFilters.Add((d, r) =>
            {
                string basePath = Environment.GetEnvironmentVariable("SWAGGER_BASEPATH");
                if (!string.IsNullOrEmpty(basePath))
                    d.BasePath = basePath;
            }));

            // Enable middle-ware to serve swagger-ui (HTML, JS, CSS etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("../api-docs/swagger.json", "Imaging API V1");
                    c.SwaggerEndpoint("v1/swagger.json", "Imaging API V1-dev");
                });

            app.UseCors(
                builder => builder.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());

            app.UseMvc();

            var busControl = app.ApplicationServices.GetService<IBusControl>();

            busControl.Start();

            appLifetime.ApplicationStopping.Register(() => busControl.Stop());
        }
    }
}
