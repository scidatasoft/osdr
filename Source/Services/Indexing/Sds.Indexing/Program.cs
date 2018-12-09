using Microsoft.Extensions.PlatformAbstractions;
using PeterKottas.DotNetCore.WindowsService;
using Serilog;
using System;
using System.IO;

namespace Sds.Osdr.Indexing
{
    class Program
    {
        public static void Main(string[] args)
        {
            ServiceRunner<IndexingService>.Run(config =>
            {
                config.SetName(IndexingService.Name);
                config.SetDisplayName(IndexingService.Title);
                config.SetDescription(IndexingService.Description);

                var name = config.GetDefaultName();

                config.Service(serviceConfig =>
                {
                    serviceConfig.ServiceFactory((extraArguments, controller) =>
                    {
                        return new IndexingService();
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