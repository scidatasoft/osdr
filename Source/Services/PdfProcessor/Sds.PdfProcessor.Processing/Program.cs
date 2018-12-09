using Topshelf;

namespace Sds.PdfProcessor.Processing
{
    class Program
    {
        static void Main(string[] args)
        {
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
