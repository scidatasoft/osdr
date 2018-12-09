using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Sds.Osdr.WebApi.IntegrationTests
{
	public class WebApiTraitDiscoverer : ITraitDiscoverer
	{
		public const string Category = "Web Api";

		public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
		{
			var args = (List<Object>)traitAttribute.GetConstructorArguments();
			var groups = (Array)args[0];

			foreach (var nameGroup in groups)
			{
				yield return new KeyValuePair<string, string>(Category, nameGroup.ToString());
			}
		}
	}
}
