using Serilog;
using System;
using Microsoft.Extensions.Configuration;
using Collector.Serilog.Enrichers.Assembly;
using PeterKottas.DotNetCore.WindowsService;

namespace Sds.NotificationService.Processing
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!Console.IsOutputRedirected)
            {
                Console.Title = NotificationMonitoringService.Title;
            }
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json");
            var appConfiguration = builder.Build();

            Log.Logger = new LoggerConfiguration()
                .Enrich.With<SourceSystemInformationalVersionEnricher<NotificationMonitoringService>>()
                .WriteTo.LiterateConsole()
                .ReadFrom.Configuration(appConfiguration)
                .CreateLogger();
            
            ServiceRunner<NotificationMonitoringService>.Run(config =>
            {
                config.SetName(NotificationMonitoringService.Name);
                config.SetDisplayName(NotificationMonitoringService.Title);
                config.SetDescription(NotificationMonitoringService.Description);

                var name = config.GetDefaultName();

                config.Service(serviceConfig =>
                {
                    serviceConfig.ServiceFactory((extraArtuments, microService) => 
                    {
                        return new NotificationMonitoringService(appConfiguration); 
                    });

                    serviceConfig.OnStart((service, extraParams) =>
                    {
                        service.Start();
                    });

                    serviceConfig.OnStop(service =>
                    {
                        service.Stop();
                    });

                    serviceConfig.OnError(e =>
                    {
                        Log.Error($"Service {name} errored with exception : {e.Message}");
                    });
                });
            });
        }
    }
}