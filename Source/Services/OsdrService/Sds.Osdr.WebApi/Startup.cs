using Collector.Serilog.Enrichers.Assembly;
using GreenPipes;
using MassTransit;
using MassTransit.RabbitMqTransport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
using MongoDB.Driver;
using Nest;
using Newtonsoft.Json.Serialization;
using Sds.MassTransit.Extensions;
using Sds.MassTransit.RabbitMq;
using Sds.MassTransit.Settings;
using Sds.Osdr.WebApi.ConnectedUsers;
using Sds.Osdr.WebApi.Extensions;
using Sds.Osdr.WebApi.Hubs;
using Sds.Osdr.WebApi.Swagger;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.GridFs;
using Serilog;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Linq;
using System.IO;
using System.Net;
using Sds.Osdr.WebApi.DataProviders;
using Sds.EventStore;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Sds.Serilog;
using Sds.Storage.KeyValue.Core;
using Sds.Storage.KeyValue.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using System.Net.WebSockets;
using System.Threading;
using Microsoft.AspNetCore.Http.Features;

namespace Sds.Osdr.WebApi
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
                .Enrich.FromLogContext()
                .MinimumLevel.ControlledBy(new EnvironmentVariableLoggingLevelSwitch("%OSDR_LOG_LEVEL%"))
                .CreateLogger();


            //BsonSerializer.RegisterSerializationProvider(new CustomBsonSerializationProvider());
        }

        public IConfigurationRoot Configuration { get; }
        private IServiceProvider Container { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var identityServer = Environment.ExpandEnvironmentVariables(Configuration["IdentityServer:Authority"]);

            services.AddOptions();
            services.Configure<MassTransitSettings>(Configuration.GetSection("MassTransit"));

            services.AddSingleton<IConnectedUserManager>(new ConnectedUserManager());
            services.AddTransient<FileCallbackResultExecutor>();
            services.AddSingleton(context => Bus.Factory.CreateUsingRabbitMq(x =>
            {
                var mtSettings = Container.GetService<IOptions<MassTransitSettings>>().Value;

                IRabbitMqHost host = x.Host(new Uri(Environment.ExpandEnvironmentVariables(mtSettings.ConnectionString)), h => { });

                x.UseSerilog();

                x.RegisterConsumers(host, context, e =>
                {
                    e.PrefetchCount = mtSettings.PrefetchCount;
                    e.UseInMemoryOutbox();
                });

                x.UseConcurrencyLimit(mtSettings.ConcurrencyLimit);
            }));

            services.AddAllConsumers();

            //Register bus
            var serviceProvider = services.BuildServiceProvider();

            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IUrlHelper, UrlHelper>(factory => new UrlHelper(factory.GetService<IActionContextAccessor>().ActionContext));

            services.Configure<SingleStructurePredictionSettings>(Configuration.GetSection("SingleStructurePredictionSettings"));
            services.Configure<FeatureVectorCalculatorSettings>(Configuration.GetSection("FeatureVectorCalculatorSettings"));

            //Redis
            try
            {
                var connectionString = Environment.ExpandEnvironmentVariables(Configuration["Redis:ConnectionString"]);
                int syncTimeout = Int32.Parse(Configuration["Redis:SyncTimeout"]);
                int connectTimeout = Int32.Parse(Configuration["Redis:ConnectTimeout"]);
                int responseTimeout = Int32.Parse(Configuration["Redis:ResponseTimeout"]);

                services.AddSingleton<IKeyValueRepository>(new RedisKeyValueRepository(connectionString, syncTimeout, connectTimeout, responseTimeout));
            }
            catch (Exception e)
            {
                Log.Error($"Creating {nameof(RedisKeyValueRepository)} error: {e.Message}");
            }

            //ElasticSearch
            try
            {
                var settings = new ConnectionSettings(new Uri(Environment.ExpandEnvironmentVariables(Configuration["ElasticSearch:ConnectionString"])));
                services.AddSingleton<IElasticClient>(new ElasticClient(settings));
            }
            catch (Exception e)
            {
                Log.Error($"Creating {nameof(ElasticClient)} error: {e.Message}");
            }

            //Eventstore
            try
            {
                var settings = Environment.ExpandEnvironmentVariables(Configuration["EventStore:ConnectionString"]);
                services.AddSingleton<IEventStore>(new EventStore.EventStore(settings));
            }
            catch (Exception e)
            {
                Log.Error($"Creating {nameof(EventStore.EventStore)} error: {e.Message}");
            }

            //services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(Environment.ExpandEnvironmentVariables(Configuration["Redis:ConnectionString"])));
            //services.AddScoped(s => s.GetService<IConnectionMultiplexer>().GetDatabase());
            try
            {
                var connectionString = Environment.ExpandEnvironmentVariables(Configuration["OsdrConnectionSettings:ConnectionString"]);
                services.AddTransient<IBlobStorage, GridFsStorage>(
                    x => new GridFsStorage(connectionString, Configuration["OsdrConnectionSettings:DatabaseName"])
                );
                Log.Information($"Connecting to MongoDB {connectionString}");
                var mongoClient = new MongoClient(connectionString);
                services.AddSingleton(mongoClient);
                var database = Configuration["OsdrConnectionSettings:DatabaseName"];
                Log.Information($"Using to MongoDB database {database}");
                services.AddSingleton(service => service.GetService<MongoClient>().GetDatabase(database));
                services.AddTransient<IOrganizeDataProvider>(service => new OrganizeDataProvider(mongoClient.GetDatabase(database), service.GetService<IBlobStorage>()));

            }
            catch (Exception ex)
            {
                Log.Fatal("Application startup failure", ex);
                throw;
            }

            services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

            // Register the Swagger generator, defining one or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "OSDR API", Version = "v1" });

                //Set the comments path for the swagger json and ui.
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                var xmlPath = Path.Combine(basePath, "Osdr.xml");
                c.IncludeXmlComments(xmlPath);
                c.DocumentFilter<LowercaseDocumentFilter>();
                c.AddSecurityDefinition("Bearer", new ApiKeyScheme()
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });
                c.OperationFilter<SecurityRequirementsOperationFilter>();
            });
            services.AddSingleton<IUserIdProvider, OsdrUserIdProvider>();

            // Add framework services.
            services.AddMvc()
                .AddJsonOptions(opt =>
                {
                    opt.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    opt.SerializerSettings.Formatting = Formatting.Indented;
                    opt.SerializerSettings.Converters.Add(new StringEnumConverter
                    {
                        CamelCaseText = false
                    });
                });

            services.AddSignalR().AddHubOptions<OrganizeHub>(options =>
            {
                options.EnableDetailedErrors = true;
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
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime, IServiceProvider serviceProvider)
        {

            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            loggerFactory.AddSerilog();

            appLifetime.ApplicationStopped.Register(Log.CloseAndFlush);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseExceptionHandler(options =>
            {
                options.Run(async context =>
                {
                    var ex = context.Features.Get<IExceptionHandlerFeature>();
                    if (ex != null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        await context.Response.WriteAsync(ex.Error.ToString());
                    }
                });
            });

            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseWebSockets();
            app.UseCors(
                builder => builder.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()).UseWebSockets();

            //app.UseWhen(context => context.Request.Path.StartsWithSegments("/api/machinelearning/features"), appBuilder =>
            //{
            //    var fvcSettings = Container.GetService<IOptions<FeatureVectorCalculatorSettings>>().Value;
            //    appBuilder.ServerFeatures.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = fvcSettings.MaxFileSize;
            //});

            var routePrefixes = new[] { "/api/entities", "/api/nodes", "/api/usernotifications", "/api/public", "/api/machinelearning/predictions" };
            var binaryDataEndpints = new[] { "/blobs/", "/images/", ".zip" };
            app.UseWhen(context =>
                    (routePrefixes.Any(context.Request.Path.Value.ToLower().Contains) && !binaryDataEndpints.Any(context.Request.Path.Value.ToLower().Contains)),
                mapApp =>
                {
                    mapApp.Use(async (context, next) =>
                    {
                        var originalBody = context.Response.Body;

                        using (var newBody = new MemoryStream())
                        {
                            // We set the response body to our stream so we can read after the chain of middlewares have been called.
                            context.Response.Body = newBody;

                            await next();

                            newBody.Seek(0, SeekOrigin.Begin);

                            string newContent = new StreamReader(newBody).ReadToEnd().Replace("\"_id\":", "\"id\":");

                            context.Response.Body = originalBody;

                            // Send our modified content to the response body.
                            await context.Response.WriteAsync(newContent);
                        }
                    });
                });

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger(c => c.PreSerializeFilters.Add((d, r) =>
            {
                string basePath = Environment.GetEnvironmentVariable("SWAGGER_BASEPATH");
                if (!string.IsNullOrEmpty(basePath))
                    d.BasePath = basePath;
            }));

            // Enable middleware to serve swagger-ui (HTML, JS, CSS etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c => c.SwaggerEndpoint("v1/swagger.json", "OSDR API V1"));

            app.UseMvc();

            app.UseSignalR(routes =>
            {
                routes.MapHub<OrganizeHub>("/signalr");
            });

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/signalr")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await Echo(context, webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }
            });

            Container = app.ApplicationServices;

            JsonConvert.DefaultSettings = (() =>
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new StringEnumConverter());

                return settings;
            });

            var bus = app.ApplicationServices.GetService<IBusControl>();
            bus.Start();

            appLifetime.ApplicationStopping.Register(() =>
            {
                Log.Information("Stopping OSDR Web API...");
                Log.Information("Stopping bus...");
                bus.Stop();
                Log.Information("Bus stopped.");
            });
        }

        private async Task Echo(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
    }
}