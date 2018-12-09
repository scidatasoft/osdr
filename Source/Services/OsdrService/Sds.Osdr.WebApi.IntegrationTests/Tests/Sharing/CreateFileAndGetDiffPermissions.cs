using FluentAssertions;
using Newtonsoft.Json.Linq;
using Sds.Osdr.IntegrationTests;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests.Tests.Sharing
{
    public class CreateFileAndGetDiffPermissionsFixture
    {
        public Guid JohnFileId { get; }
        public Guid JaneFileId { get; }

        public CreateFileAndGetDiffPermissionsFixture(OsdrWebTestHarness harness)
        {
            JohnFileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "Aspirin.mol", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;
            JaneFileId = harness.ProcessRecordsFile(harness.JaneId.ToString(), "ringcount_0.mol", new Dictionary<string, object>() { { "parentId", harness.JaneId } }).Result;

            var janeFile = harness.Session.Get<RecordsFile.Domain.RecordsFile>(JaneFileId).Result;
            harness.JaneApi.SetPublicFileEntity(JaneFileId, janeFile.Version, true).GetAwaiter().GetResult();
            harness.WaitWhileFileShared(JaneFileId);
        }
    }

    [Collection("OSDR Test Harness")]
    public class CreateFileAndGetDiffPermissions : OsdrWebTest, IClassFixture<CreateFileAndGetDiffPermissionsFixture>
    {
        private Guid JohnFileId { get; }
        private Guid JaneFileId { get; }

        public CreateFileAndGetDiffPermissions(OsdrWebTestHarness fixture, ITestOutputHelper output, CreateFileAndGetDiffPermissionsFixture initFixture) : base(fixture, output)
        {
            JohnFileId = initFixture.JohnFileId;
            JaneFileId = initFixture.JaneFileId;
        }
        
        [Fact(Skip = "Broken"), WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithAuthorizeUser_ReturnsExpectedAllNodes()
        {
            var fileResponse = await JohnApi.GetNodes();
            var nodes = JToken.Parse(await fileResponse.Content.ReadAsStringAsync());
            
            nodes.Should().HaveCount(4);

            var fileNames = new[]
            {
                "ringcount_0.mol",
                "Aspirin.mol"
            };
            
            foreach (var node in nodes)
            {
                if (node["type"].ToObject<string>() == "File")
                {
                    fileNames.Contains(node["name"].ToObject<string>()).Should().BeTrue();
                }
            }
        }
        
        [Fact(Skip = "Broken"), WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithAuthorizeUser_ReturnsExpectedPublicNodes()
        {
            var response = await JaneApi.GetNodes();
            var nodes = JToken.Parse(await response.Content.ReadAsStringAsync());

            nodes.Should().HaveCount(2);
            
            var fileNames = new[]
            {
                "ringcount_0.mol"
            };
            
            foreach (var node in nodes)
            {
                if (node["type"].ToObject<string>() == "File")
                {
                    fileNames.Contains(node["name"].ToObject<string>()).Should().BeTrue();
                }
            }
        }
        
        [Fact(Skip = "Broken"), WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithUnAuthorizeUser_ReturnsExpectedPublicNodes()
        {
            var response = await UnauthorizedApi.GetNodes();
            var nodes = JToken.Parse(await response.Content.ReadAsStringAsync());

            nodes.Should().HaveCount(2);
            
            var fileNames = new[]
            {
                "ringcount_0.mol"
            };
            
            foreach (var node in nodes)
            {
                if (node["type"].ToObject<string>() == "File")
                {
                    fileNames.Contains(node["name"].ToObject<string>()).Should().BeTrue();
                }
            }
        }
        
        [Fact(Skip = "Broken"), WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_WithUnAuthorizeUser_ReturnsExpectedPublicNodesFilter()
        {
            var response = await UnauthorizedApi.GetNodes("Type eq 'File'"); // TODO: Refactoring
            var nodes = JToken.Parse(await response.Content.ReadAsStringAsync());

            nodes.Should().HaveCount(1);
            
            var fileNames = new[]
            {
                "ringcount_0.mol"
            };
            
            foreach (var node in nodes)
            {
                if (node["type"].ToObject<string>() == "File")
                {
                    fileNames.Contains(node["name"].ToObject<string>()).Should().BeTrue();
                }
            }
        }
    }
}