using Microsoft.Extensions.Configuration;
using PeterKottas.DotNetCore.WindowsService;
using Serilog;

namespace Sds.ChemicalStandardizationValidation.Persistance
{
    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }

        static void Main(string[] args)
        {
            ServiceRunner<PersistenceService>.Run(config =>
            {
                config.SetName(PersistenceService.Name);
                config.SetDisplayName(PersistenceService.Title);
                config.SetDescription(PersistenceService.Description);

                var name = config.GetDefaultName();

                config.Service(serviceConfig =>
                {
                    serviceConfig.ServiceFactory((extraArguments, service) =>
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