using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using IdentityModel.AspNetCore.OAuth2Introspection;
using MassTransit;
using MassTransit.RabbitMqTransport;
using MongoDB.Driver;
using Sds.MassTransit.Observers;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using Collector.Serilog.Enrichers.Assembly;
using Microsoft.Extensions.PlatformAbstractions;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;

namespace MetadataStorage
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
                .Enrich.With<SourceSystemInformationalVersionEnricher<Startup>>()
                .ReadFrom.Configuration(Configuration)
                .Enrich.FromLogContext()
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

            try
            {
                var connectionString = Environment.ExpandEnvironmentVariables(Configuration["OsdrConnectionSettings:ConnectionString"]);
                Log.Information($"Connecting to MongoDB {connectionString}");
                services.AddSingleton(new MongoClient(connectionString));
                var database = Configuration["OsdrConnectionSettings:DatabaseName"];
                Log.Information($"Using to MongoDB database {database}");
                services.AddScoped(service => service.GetService<MongoClient>().GetDatabase(database));
            }
            catch (Exception ex)
            {
                Log.Fatal("Application startup failure", ex);
                throw;
            }

            // Add framework services.
            services
                .AddMvc()
                .AddJsonOptions(opt =>
                            {
                                opt.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                                opt.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
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
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            loggerFactory.AddSerilog();

            app.UseAuthentication();

            app.UseCors(
                 builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());

            app.UseMvc();

            var busControl = app.ApplicationServices.GetService<IBusControl>();
            busControl.ConnectPublishObserver(new PublishObserver());
            busControl.ConnectConsumeObserver(new ConsumeObserver());
            
            busControl.Start();

            appLifetime.ApplicationStopping.Register(() => busControl.Stop());
        }
    }
}
