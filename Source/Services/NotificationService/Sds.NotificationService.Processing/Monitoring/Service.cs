using System;
using Newtonsoft.Json;

namespace Sds.NotificationService.Processing.Monitoring
{
    public class Service
    {
        public DateTime TimeStamp { get; }
        public string Host { get; }
        public string Monitor { get; }
        public string Type { get; }
        public bool Up { get; }
        public string Error { get; }
        
        public Service(string json)
        {
            json = json.Replace("@", "");
            var jsonDocument = JsonConvert.DeserializeObject<dynamic>(json);
            var sourceService = jsonDocument.hits.hits[0]._source;

            TimeStamp = sourceService.timestamp.Value;
            Host = sourceService.host?.Value;
            Monitor = sourceService.monitor.Value;
            Up = sourceService.up.Value;
            Error = sourceService?.error?.message;
            Type = sourceService.type;
        }
        
        public override string ToString()
        {
            return $"Timestamp: {TimeStamp}\nHost: {Host}\nType: {Type}\nMonitor: {Monitor}\nError: {Error}";
        }
    }
}