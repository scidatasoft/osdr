using Microsoft.Extensions.Configuration;
using PeterKottas.DotNetCore.WindowsService;
using Serilog;
using System;

namespace Sds.Osdr.Domain.BackEnd
{
    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }

        static void Main(string[] args)
        {
            if (!Console.IsOutputRedirected)
            {
                Console.Title = DomainBackEndService.Title;
            }

            ServiceRunner<DomainBackEndService>.Run(config =>
            {
                config.SetName(DomainBackEndService.Name);
                config.SetDisplayName(DomainBackEndService.Title);
                config.SetDescription(DomainBackEndService.Description);

                var name = config.GetDefaultName();

                config.Service(serviceConfig =>
                {
                    serviceConfig.ServiceFactory((extraArguments, controller) =>
                    {
                        return new DomainBackEndService();
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