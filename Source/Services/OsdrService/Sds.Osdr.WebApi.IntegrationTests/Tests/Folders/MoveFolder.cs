using FluentAssertions;
using Newtonsoft.Json.Linq;
using Sds.Osdr.IntegrationTests;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    [Collection("OSDR Test Harness")]
    public class MoveFolder : OsdrWebTest
    {
        private Guid _firstFolderId;
        private Guid _secondFolderId;

        public MoveFolder(OsdrWebTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            var firstFolder = JohnApi.CreateFolderEntity(JohnId, "folder 1").Result;
            var secondFolder = JohnApi.CreateFolderEntity(JohnId, "folder 1").Result;

            var firstFolderLocation = firstFolder.Headers.Location.ToString();
            _firstFolderId = Guid.Parse(firstFolderLocation.Substring(firstFolderLocation.LastIndexOf("/") + 1));

            var secondFolderLocation = secondFolder.Headers.Location.ToString();
            _secondFolderId = Guid.Parse(secondFolderLocation.Substring(secondFolderLocation.LastIndexOf("/") + 1));

            Harness.WaitWhileFolderCreated(_firstFolderId);
            Harness.WaitWhileFolderCreated(_secondFolderId);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Folder)]
        public async Task FolderOperation_MoveFolder_MovedFolder()
        {
            var responseFirst = await JohnApi.GetFolder(_firstFolderId);
            responseFirst.EnsureSuccessStatusCode();
            var jsonFirstFolder = JToken.Parse(await responseFirst.Content.ReadAsStringAsync());
	        
            var responseSecond = await JohnApi.GetFolder(_secondFolderId);
            responseSecond.EnsureSuccessStatusCode();
            var jsonSecondFolder = JToken.Parse(await responseSecond.Content.ReadAsStringAsync());

            var folders = new Dictionary<string, JToken>
            {
                { "first", jsonFirstFolder },
                { "second", jsonSecondFolder }
            };
			
            foreach (var dirFolder in folders)
            {
                var folder = dirFolder.Value;
                folder.Should().NotBeNullOrEmpty();
                folder["parentId"].ToString().Should().BeEquivalentTo(JohnId.ToString());
            }

            await JohnApi.MoveFolder(folders["first"]["id"].ToObject<Guid>(), folders["first"]["version"].ToObject<int>(), folders["second"]["id"].ToObject<Guid>());

            Harness.WaitWhileFolderMoved(_firstFolderId);

            folders["first"] = JToken.Parse(await JohnApi.GetFolder(folders["first"]["id"].ToObject<Guid>())
                .GetAwaiter()
                .GetResult()
                .Content
                .ReadAsStringAsync());
            folders["second"] = JToken.Parse(await JohnApi.GetFolder(folders["second"]["id"].ToObject<Guid>())
                .GetAwaiter()
                .GetResult()
                .Content
                .ReadAsStringAsync());

            folders["first"].Should().ContainsJson($@"
			{{
				'id': '{folders["first"]["id"]}',
				'createdBy': '{JohnId}',
				'createdDateTime': *EXIST*,
				'updatedBy': '{JohnId}',
				'updatedDateTime': *EXIST*,
				'parentId': '{folders["second"]["id"].ToObject<Guid>()}',
				'ownedBy': '{JohnId}',
				'name': 'folder 1',
				'status': 'Created',
				'version': 2
			}}");
        }
    }
}