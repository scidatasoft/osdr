using Microsoft.Extensions.Configuration;
using PeterKottas.DotNetCore.WindowsService;
using Serilog;
using System;

namespace Sds.Osdr.Domain.SagaHost
{
    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }

        static void Main(string[] args)
        {
            if (!Console.IsOutputRedirected)
            {
                Console.Title = DomainSagaHostService.Title;
            }

            ServiceRunner<DomainSagaHostService>.Run(config =>
            {
                config.SetName(DomainSagaHostService.Name);
                config.SetDisplayName(DomainSagaHostService.Title);
                config.SetDescription(DomainSagaHostService.Description);

                var name = config.GetDefaultName();

                config.Service(serviceConfig =>
                {
                    serviceConfig.ServiceFactory((extraArguments, controller) =>
                    {
                        return new DomainSagaHostService();
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