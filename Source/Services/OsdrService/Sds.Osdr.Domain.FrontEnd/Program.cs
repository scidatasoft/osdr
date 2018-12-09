using Microsoft.Extensions.Configuration;
using PeterKottas.DotNetCore.WindowsService;
using Serilog;
using System;

namespace Sds.Osdr.Domain.FrontEnd
{
    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }

        static void Main(string[] args)
        {
            if (!Console.IsOutputRedirected)
            {
                Console.Title = DomainFrontEndService.Title;
            }
            ServiceRunner<DomainFrontEndService>.Run(config =>
            {
                config.SetName(DomainFrontEndService.Name);
                config.SetDisplayName(DomainFrontEndService.Title);
                config.SetDescription(DomainFrontEndService.Description);

                var name = config.GetDefaultName();

                config.Service(serviceConfig =>
                {
                    serviceConfig.ServiceFactory((extraArguments, controller) =>
                    {
                        return new DomainFrontEndService();
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