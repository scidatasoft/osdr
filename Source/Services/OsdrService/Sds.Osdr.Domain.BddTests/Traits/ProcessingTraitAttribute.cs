using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Sds.Osdr.BddTests.Traits
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
