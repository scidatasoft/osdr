using FluentAssertions;
using Newtonsoft.Json.Linq;
using Sds.Osdr.IntegrationTests;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    [Collection("OSDR Test Harness")]
    public class RenameFolder : OsdrWebTest
    {
        private Guid _folderId;

		public RenameFolder(OsdrWebTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
		{
            var response = JohnApi.CreateFolderEntity(JohnId, "new folder").Result;
            var folderLocation = response.Headers.Location.ToString();
            _folderId = Guid.Parse(folderLocation.Substring(folderLocation.LastIndexOf("/") + 1));

            Harness.WaitWhileFolderCreated(_folderId);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Folder)]
		public async Task FolderOperation_RenameFolder_ExpectedRenamedFolder()
		{
			await JohnApi.RenameFolder(_folderId, "renamed folder");

			Harness.WaitWhileFolderRenamed(_folderId);

			var response = await JohnApi.GetFolder(_folderId);
            response.EnsureSuccessStatusCode();
            var jsonFolder = JToken.Parse(await response.Content.ReadAsStringAsync());

			jsonFolder.Should().ContainsJson($@"
			{{
				'id': '{_folderId}',
				'createdBy': '{JohnId}',
				'createdDateTime': *EXIST*,
				'updatedBy': '{JohnId}',
				'updatedDateTime': *EXIST*,
				'ownedBy': '{JohnId}',
				'name': 'renamed folder',
				'status': 'Created',
				'version': 2
			}}");
		}
    }
}
