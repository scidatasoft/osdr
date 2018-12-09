using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace Sds.Storage.Blob.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            //var cert = new X509Certificate2("osdr.pfx", "your-company");

            var host = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseConfiguration(configuration)
                .UseIISIntegration()
                .UseKestrel(cfg => {
                    //cfg.UseHttps(cert);
                })
                .UseStartup<Startup>()
                //.UseApplicationInsights()
                .Build();

            host.Run();
        }
    }
}