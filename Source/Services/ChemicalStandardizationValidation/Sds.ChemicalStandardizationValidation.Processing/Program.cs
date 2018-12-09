using Collector.Serilog.Enrichers.Assembly;
using Sds.Serilog;
using Serilog;
using Topshelf;

namespace Sds.ChemicalStandardizationValidation.Processing
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.With<SourceSystemInformationalVersionEnricher<ServiceProcessingControl>>()
                .ReadFrom.AppSettings()
                .MinimumLevel.ControlledBy(new EnvironmentVariableLoggingLevelSwitch("%OSDR_LOG_LEVEL%"))
                .CreateLogger();

            HostFactory.Run(cfg =>
            {
                cfg.EnableServiceRecovery(r =>
                {
                    r.RestartService(5);
                    r.OnCrashOnly();
                });
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
