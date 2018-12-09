using Collector.Serilog.Enrichers.Assembly;
using Microsoft.Extensions.Configuration;
using PeterKottas.DotNetCore.WindowsService;
using Sds.Serilog;
using Serilog;

namespace Sds.Imaging.Persistence
{
    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json");

            Configuration = builder.Build();

            Log.Logger = new LoggerConfiguration()
                .Enrich.With<SourceSystemInformationalVersionEnricher<PersistenceService>>()
                .ReadFrom.Configuration(Configuration)
                .MinimumLevel.ControlledBy(new EnvironmentVariableLoggingLevelSwitch("%OSDR_LOG_LEVEL%"))
                .CreateLogger();

            ServiceRunner<PersistenceService>.Run(config =>
            {
                config.SetName(PersistenceService.Name);
                config.SetDisplayName(PersistenceService.Title);
                config.SetDescription(PersistenceService.Description);

                var name = config.GetDefaultName();

                config.Service(serviceConfig =>
                {
                    serviceConfig.ServiceFactory((extraArguments, controller) =>
                    {
                        return new PersistenceService();
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