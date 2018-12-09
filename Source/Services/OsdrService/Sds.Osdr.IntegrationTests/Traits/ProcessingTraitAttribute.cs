using System;
using Xunit.Sdk;

namespace Sds.Osdr.IntegrationTests.Traits
{
    [TraitDiscoverer("Sds.Osdr.BddTests.Traits.ProcessingTraitDiscoverer", "Sds.Osdr.Domain.BddTests")]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public class ProcessingTraitAttribute : Attribute, ITraitAttribute
	{
		public ProcessingTraitAttribute(params TraitGroup[] group)
		{
		}
	}
}
