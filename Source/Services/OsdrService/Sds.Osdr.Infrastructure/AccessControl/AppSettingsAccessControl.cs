using Microsoft.Extensions.Options;
using Sds.Osdr.Domain.AccessControl;
using System.Linq;

namespace Sds.Osdr.Infrastructure.AccessControl
{
    public class Service
    {
        public string Name { get; set; }
        public bool Available { get; set; }
    }

    public class AccessControl
    {
        public Service[] Services { get; set; }
    }

    public class AppSettingsAccessControl : IAccessControl
    {
        private AccessControl _accessControl;

        public AppSettingsAccessControl(IOptions<AccessControl> accessControl)
        {
            _accessControl = accessControl.Value;
        }

        public bool IsServiceAvailable<T>() where T : class
        {
            if (_accessControl.Services != null && _accessControl.Services.Any(s => s.Name.Equals(typeof(T).FullName)))
            {
                return _accessControl.Services.First(s => s.Name.Equals(typeof(T).FullName)).Available;
            }

            return true;
        }
    }
}
