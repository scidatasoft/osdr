using Sds.Osdr.IntegrationTests.Traits;
using System;
using Xunit.Sdk;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    [TraitDiscoverer("Sds.Osdr.WebApi.IntegrationTests.WebApiTraitDiscoverer", "Sds.Osdr.WebApi.IntegrationTests")]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public class WebApiTraitAttribute : Attribute, ITraitAttribute
	{
		public WebApiTraitAttribute(params TraitGroup[] group)
		{
		}
	}
}
