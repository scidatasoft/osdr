using PeterKottas.DotNetCore.WindowsService;
using Serilog;
using System;
using System.Runtime.InteropServices;


namespace Sds.Osdr.Persistence
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!Console.IsOutputRedirected)
            {
                Console.Title = PersistenceService.Title;
            }
            
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