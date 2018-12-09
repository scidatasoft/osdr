using PeterKottas.DotNetCore.WindowsService;
using Serilog;
using System;

namespace Sds.MetadataStorage.Processing
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceRunner<ProcessingService>.Run(config =>
            {
                config.SetName(ProcessingService.Name);
                config.SetDisplayName(ProcessingService.Title);
                config.SetDescription(ProcessingService.Description);

                var name = config.GetDefaultName();

                config.Service(serviceConfig =>
                {
                    serviceConfig.ServiceFactory((extraArguments, controller) =>
                    {
                        return new ProcessingService();
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
                        Log.Error($"Service {name} start failed with exception : {e.Message}");
                    });
                });
            });
        }
    }
}
