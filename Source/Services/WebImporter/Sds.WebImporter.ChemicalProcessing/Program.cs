using Serilog;
using Topshelf;

namespace Sds.WebImporter.ChemicalProcessing
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .CreateLogger();

            HostFactory.Run(cfg =>
            {
                cfg.SetServiceName(ServiceProcessingControl.Name);
                cfg.SetDescription(ServiceProcessingControl.Description);
                cfg.SetDisplayName(ServiceProcessingControl.Title);
                cfg.UseSerilog();
                cfg.Service<ServiceProcessingControl>();
                cfg.RunAsLocalSystem();
            });
        }
    }
}
