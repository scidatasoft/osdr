namespace Sds.Osdr.WebApi.Extensions
{
    public class RedisSettings
    {
        public RedisSettings() { }

        public string ConnectionString { get; set; } = "%OSDR_REDIS%";
        public int SyncTimeout { get; set; } = 10000;
        public int ConnectTimeout { get; set; } = 10000;
        public int ResponseTimeout { get; set; } = 10000;
    }
}
