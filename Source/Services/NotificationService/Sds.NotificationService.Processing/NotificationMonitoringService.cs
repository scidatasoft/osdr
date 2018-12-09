using System;
using PeterKottas.DotNetCore.WindowsService.Base;
using PeterKottas.DotNetCore.WindowsService.Interfaces;
using System.Reflection;
using Sds.Reflection;
using Serilog;
using Microsoft.Extensions.Configuration;
using System.Text;
using Elasticsearch.Net;
using Newtonsoft.Json;
using System.Collections.Generic;
using Sds.NotificationService.Processing.Monitoring;
using Sds.NotificationService.Processing.Notification;

using Timer = System.Timers.Timer;

namespace Sds.NotificationService.Processing
{
    public class NotificationMonitoringService : MicroService, IMicroService
    {
        public static string Name => Assembly.GetEntryAssembly().GetName().Name;
        public static string Title => Assembly.GetEntryAssembly().GetTitle();
        public static string Description => Assembly.GetEntryAssembly().GetDescription();
        public static string Version => Assembly.GetEntryAssembly().GetVersion();
        public Timer Timer;
        public IConfiguration Configuration { get; set; }
        public NotificationMonitoringService(IConfiguration configuration) => Configuration = configuration;
        public ElasticLowLevelClient Client { get; set; }
        public RepositoryServices Repository { get; set; }
        public MailNotification Notification { get; set; }

        public void Start()
        {
            Log.Information($"Service {Name} v.{Version} starting...");

            Log.Information($"Name: {Name}");
            Log.Information($"Title: {Title}");
            Log.Information($"Description: {Description}");
            Log.Information($"Version: {Version}");

            var services = new List<string>();

            Log.Information("Monitoring for");
            for (var i = 0; Configuration[$"checkServices:services:{i}"] != null; i++)
            {
                var serviceName = Configuration[$"checkServices:services:{i}"];
                services.Add(serviceName);

                Log.Information($"{i+1}. {serviceName}");
            }

            var host = $"http://{Configuration["remoteHost:name"]}:{Configuration["remoteHost:port"]}";
            Log.Information($"Host: {host}");
            var settings = new ConnectionConfiguration(new Uri(host))
                .PrettyJson()
                .DisableDirectStreaming()
                .OnRequestCompleted(response =>
                {
                });


            Notification = new MailNotification(Configuration);
            Client = new ElasticLowLevelClient(settings);
            Repository = new RepositoryServices(Client, services.ToArray());

            Timer = new Timer
            {
                Interval = int.Parse(Configuration["checkServices:interval"]) * 1000
            };
            Timer.Elapsed += (o, e) => Update();
            Timer.Start();

            //	Update();
        }
        
        public void Update()
        {
            Repository.Update();

            if (Repository.NoUpService.Count > 0)
                Notification.Notify(Repository.NoUpService);
        }

        public void Stop()
        {
            Timer.Stop();
        }
    }
}