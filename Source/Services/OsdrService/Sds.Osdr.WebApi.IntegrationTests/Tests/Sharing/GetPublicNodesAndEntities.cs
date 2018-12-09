using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests.Tests.Sharing
{
    public class GetPublicNodesAndEntitiesFixture
    {
        public List<Guid> InternalIds { get; set; }
        public Dictionary<string, Guid> FoldersId { get; set; }
        private bool _isInitialize = false;

        public GetPublicNodesAndEntitiesFixture()
        {
        }

        public void Initialize(Action action)
        {
            if (!_isInitialize)
            {
                _isInitialize = true;

                InternalIds = new List<Guid>();
                FoldersId = new Dictionary<string, Guid>();

                action();
            }
        }
    }


    [Collection("OSDR Test Harness")]
    public class GetPublicNodesAndEntities : OsdrWebTest, IClassFixture<GetPublicNodesAndEntitiesFixture>
    {
        private GetPublicNodesAndEntitiesFixture _testFixture;

        public GetPublicNodesAndEntities(GetPublicNodesAndEntitiesFixture testFixture, OsdrWebTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            _testFixture = testFixture;

            _testFixture.Initialize(() =>
            {
                AddFolder("First", JohnId);
                AddFolder("First-Second", _testFixture.FoldersId["First"]);

                AddPrivateFile("125_11Mos.cdx", JohnId).GetAwaiter().GetResult();
                AddPrivateFile("test_solubility.sdf", _testFixture.FoldersId["First"]).GetAwaiter().GetResult();

                AddPublicFile("Aspirin.mol", _testFixture.FoldersId["First"]).GetAwaiter().GetResult();
                AddPublicFile("ringcount_0.mol", _testFixture.FoldersId["First-Second"]).GetAwaiter().GetResult();
//            AddPublicFile("125_11mos.cdx", FoldersId["First"]).GetAwaiter().GetResult();
            });
        }
        
        private Guid AddFolder(string name, Guid parentId)
        {
            var response = JohnApi.CreateFolderEntity(parentId, name).Result;
            var folderLocation = response.Headers.Location.ToString();
            var _folderId = Guid.Parse(folderLocation.Substring(folderLocation.LastIndexOf("/") + 1));

            Harness.WaitWhileFolderCreated(_folderId);
            
            _testFixture.FoldersId.Add(name, _folderId);
            
            _testFixture.InternalIds.Add(_folderId);
            
            return _folderId;
        }

        private async Task<Guid> AddPublicFile(string fileName, Guid parentId) =>
            await AddFile(true, fileName, parentId);
        
        private async Task<Guid> AddPrivateFile(string fileName, Guid parentId) =>
            await AddFile(false, fileName, parentId);

        private async Task<Guid> AddFile(bool isPublic, string fileName, Guid parentId)
        {
            var fileId = ProcessRecordsFile(JohnId.ToString(), fileName, new Dictionary<string, object>() { { "parentId", parentId } }).Result;

            await ChangeShareFile(fileId, isPublic);

            var recordsId = await Records.FindAsync(new BsonDocument("FileId", fileId))
                .GetAwaiter()
                .GetResult()
                .ToListAsync();

            foreach (var record in recordsId)
            {
                if (!_testFixture.InternalIds.Contains((Guid) record._id))
                    _testFixture.InternalIds.Add(record._id);
            }
          //  InternalIds.AddRange(recordsId.Select(x => (Guid)x._id));
	        
            if (!_testFixture.InternalIds.Contains((Guid) fileId))
                _testFixture.InternalIds.Add(fileId);

            return fileId;
        }

        private async Task<Guid> ChangeShareFile(Guid fileId, bool isPublic)
        {
            var file = await Session.Get<RecordsFile.Domain.RecordsFile>(fileId);
            var response = await JohnApi.SetPublicFileEntity(fileId, file.Version, isPublic);
            
            Harness.WaitWhileFileShared(fileId);

            return file.Id;
        }
        
        private async Task<Guid> ChangeShareFolder(Guid folderId, bool isPublic)
        {
            var folder = await Session.Get<Generic.Domain.Folder>(folderId);
            var response = await JohnApi.SetPublicFoldersEntity(folderId, folder.Version, isPublic);
            
            Harness.WaitWhileFolderShared(folder.Id);
            
            return folder.Id;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_CheckPageNodes_ReturnsPaginationHeader()
        {
            const int currentPage = 1;
            const int pageSize = 100;

            var response = await JohnApi.GetPublicNodes(currentPage, pageSize);
            response.EnsureSuccessStatusCode();

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            var totalCount = json.ContainsNodes(_testFixture.InternalIds);
            totalCount.ShouldBeEquivalentTo(2);
	        
            var paginationHeader = response.Headers.GetValues("X-Pagination").Single();
            var jsonPagination = JObject.Parse(paginationHeader);
            jsonPagination.Should().ContainsJson($@"
            {{
                'pageSize': {pageSize},
                'totalPages': 1,
                'currentPage': {currentPage}
            }}");
//                'totalCount': {totalCount},
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_CheckPageNodes_ReturnsPagesWithParameters()
        {
            const int currentPage = 1;
            const int pageSize = 2;

            var response = await JohnApi.GetPublicNodes(currentPage, pageSize);
            response.EnsureSuccessStatusCode();

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            var totalCount = json.ContainsNodes(_testFixture.InternalIds);
            totalCount.ShouldBeEquivalentTo(2);
	        
            var paginationHeader = response.Headers.GetValues("X-Pagination").Single();
            var jsonPagination = JObject.Parse(paginationHeader);
            jsonPagination.Should().ContainsJson($@"
            {{
                'pageSize': {pageSize},
                'totalPages': 1,
                'currentPage': {currentPage}
            }}");
//                'totalCount': {totalCount},
        }
	    
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_CheckPageForNodesMe_ReturnsPaginationHeader()
        {
            const int currentPage = 1;
            const int pageSize = 100;

            var response = await JohnApi.GetPublicNodes(currentPage, pageSize);
            response.EnsureSuccessStatusCode();

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            var totalCount = json.ContainsNodes(_testFixture.InternalIds);
            totalCount.ShouldBeEquivalentTo(2);
	        
            var paginationHeader = response.Headers.GetValues("X-Pagination").Single();
            var jsonPagination = JObject.Parse(paginationHeader);
            jsonPagination.Should().ContainsJson($@"
            {{
                'pageSize': {pageSize},
                'totalPages': 1,
                'currentPage': {currentPage}
            }}");
//                'totalCount': {json.Count()},
        }
	    
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_CheckPageForEntityMe_ReturnsPaginationHeader()
        {
            const int currentPage = 1;
            const int pageSize = 100;

            var response = await JohnApi.GetPublicEntity("files", currentPage, pageSize);
            response.EnsureSuccessStatusCode();

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            var totalCount = json.ContainsNodes(_testFixture.InternalIds);
            totalCount.ShouldBeEquivalentTo(2);
	        
            var paginationHeader = response.Headers.GetValues("X-Pagination").Single();
            var jsonPagination = JObject.Parse(paginationHeader);
            jsonPagination.Should().ContainsJson($@"
            {{
                'pageSize': {pageSize},
                'totalPages': 1,
                'currentPage': {currentPage}
            }}");
//                'totalCount': {json.Count()},
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_GetPublicChemicalStructures_ReturnTwoNodes()
        {
            var response = await JohnApi.GetPublicNodes(1, 2, "Name eq 'Aspirin.mol'");
            response.EnsureSuccessStatusCode();

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            json.ContainsNodes(_testFixture.InternalIds).ShouldBeEquivalentTo(1);
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_GetPublicPageSize1_ReturnOneNodes()
        {
            var response = await JohnApi.GetPublicNodes(1, 1);
            response.EnsureSuccessStatusCode();

            var json = JToken.Parse(await response.Content.ReadAsStringAsync());
            json.ContainsNodes(_testFixture.InternalIds).ShouldBeEquivalentTo(1);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.NotAuthorized)]
        public async Task FileSharing_UnauthorizedRequestForNodes_ReturnsExpectedResults()
        {
            var response = await UnauthorizedApi.GetPublicNodes();
            response.EnsureSuccessStatusCode();

            var nodes = JToken.Parse(await response.Content.ReadAsStringAsync());
            nodes.ContainsNodes(_testFixture.InternalIds).ShouldBeEquivalentTo(2);
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.NotAuthorized)]
        public async Task FileSharing_UnauthorizedRequestForEntitiesFiles_ReturnsExpectedResults()
        {
            var response = await UnauthorizedApi.GetPublicEntity("Files");
            response.EnsureSuccessStatusCode();

            var nodes = JToken.Parse(await response.Content.ReadAsStringAsync());
            nodes.ContainsNodes(_testFixture.InternalIds).ShouldBeEquivalentTo(2);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_FindRootFolder_ReturnsOneFolderAndFourFiles()
        {
            var response = await JohnApi.GetPublicNodes();
            response.EnsureSuccessStatusCode();

            var nodes = JToken.Parse(await response.Content.ReadAsStringAsync());
            nodes.ContainsNodes(_testFixture.InternalIds).ShouldBeEquivalentTo(2);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_GetPropertiesWithProjection_ReturnsNodesOnlyName()
        {
            var response = await JohnApi.GetPublicNodes(1, 2, null, "Name");
            response.EnsureSuccessStatusCode();

            var nodes = JToken.Parse(await response.Content.ReadAsStringAsync());
            nodes.ContainsNodes(_testFixture.InternalIds).ShouldBeEquivalentTo(2);

            var fileNames = new List<string>() { "Aspirin.mol", "0", "ringcount_0.mol", "0" };
	        
            foreach (var node in nodes)
            {
                node.Should().HaveCount(2);
            }
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_GetEmptyFolders_ReturnsOnlyEmptyFolders()
        {
            var response = await JohnApi.GetPublicEntity("Folders");
            response.EnsureSuccessStatusCode();

            var folders = JToken.Parse(await response.Content.ReadAsStringAsync());
            folders.ContainsNodes(_testFixture.InternalIds).ShouldBeEquivalentTo(0);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_GetTwoFiles_ReturnsOnlyTwoFiles()
        {
            var response = await JohnApi.GetPublicEntity("Files");
            response.EnsureSuccessStatusCode();

            var files = JToken.Parse(await response.Content.ReadAsStringAsync());
            files.ContainsNodes(_testFixture.InternalIds).ShouldBeEquivalentTo(2);
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_GetThreeRecords_ReturnsOnlyThreeRecords()
        {
            var response = await JohnApi.GetPublicEntity("Records");
            response.EnsureSuccessStatusCode();

            var records = JToken.Parse(await response.Content.ReadAsStringAsync());
            records.ContainsNodes(_testFixture.InternalIds).ShouldBeEquivalentTo(2);
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_UseFilter_ReturnsOnlyIndexTwo()
        {
            var response = await JohnApi.GetPublicEntity("Records", 1, 20, "Status eq 'Failed'");
            response.EnsureSuccessStatusCode();

            var files = JToken.Parse(await response.Content.ReadAsStringAsync());
            files.ContainsNodes(_testFixture.InternalIds).ShouldBeEquivalentTo(1);
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_UseProjection_ReturnsFieldOnlyType()
        {
            var response = await JohnApi.GetPublicEntity("Records", 1, 20, null, "Type");
            response.EnsureSuccessStatusCode();

            var files = JToken.Parse(await response.Content.ReadAsStringAsync());
            files.ContainsNodes(_testFixture.InternalIds).ShouldBeEquivalentTo(2);
            
            foreach (var item in files)
            {
                item.Should().HaveCount(2);
                item["type"].ToObject<string>().ShouldBeEquivalentTo("Structure");
            }
        }

        [Fact(Skip = "not implement"), WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_SharedFolderAllowsToGetAccessToFile_ReturnsAllExpectedFile()
        {
            var fileName = "1100118.cif";
            var folderId = AddFolder("rootFolder", JohnId);
            await ChangeShareFolder(folderId, true);

            await AddFile(false, fileName, folderId);

            var response = await JohnApi.GetPublicNodes();
            response.EnsureSuccessStatusCode();
            var jsonFiles = JToken.Parse(await response.Content.ReadAsStringAsync());

            var responseFilesId = jsonFiles.Select(x => x["name"].ToObject<string>());
            responseFilesId.Contains(fileName).ShouldBeEquivalentTo(true);
        }
        
        [Fact(Skip = "not implement"), WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_SharedFolderAllowsToGetAccessToRecords_ReturnsAllExpectedRecords()
        {
            var fileName = "test_solubility.sdf";
            var folderId = AddFolder("rootFolder", JohnId);
            await ChangeShareFolder(folderId, true);

            await AddFile(false, fileName, folderId);

            var response = await JohnApi.GetPublicNodes();
            response.EnsureSuccessStatusCode();
            var jsonFiles = JToken.Parse(await response.Content.ReadAsStringAsync());
            jsonFiles.ContainsNodes(_testFixture.InternalIds).ShouldBeEquivalentTo(8);

            var responseFilesId = jsonFiles.Select(x => x["name"].ToObject<string>());
            responseFilesId.Contains(fileName).ShouldBeEquivalentTo(true);
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task FileSharing_NotSharedFolderAllowsToGetAccessToFileAndRecords_ReturnsEmpty()
        {
            var fileName = "test_solubility.sdf";
            var folderId = AddFolder("rootFolder", JohnId);

            await AddFile(false, fileName, folderId);

            var response = await JohnApi.GetPublicNodes();
            response.EnsureSuccessStatusCode();
            
            var jsonFiles = JToken.Parse(await response.Content.ReadAsStringAsync());
            jsonFiles.ContainsNodes(_testFixture.InternalIds).ShouldBeEquivalentTo(2);
            
            var responseFilesId = jsonFiles.Select(x => x["name"].ToObject<string>());
            responseFilesId.Contains(fileName).ShouldBeEquivalentTo(false);
        }

    }
}

/*
 * db.AccessPermissions.aggregate([
{
	$match: { IsPublic: true }
},
{
	$lookup: {
		from: "Nodes",
		localField: "_id",
		foreignField: "_id",
		as: "Nodes"
	}
},
{
	$graphLookup: {
		from: "Nodes",
		connectFromField: "_id",
		connectToField: "ParentId",
		startWith: "$_id",
		as: "Parents"
	}
},
{
	$project: 
	{ 
		items: 
		{ 
			$concatArrays: [ "$Nodes", "$Parents" ] 
		}
	}
},
{
	$unwind: "$items"
},
{
	$replaceRoot: { newRoot: "$items" }
},
{
	$project: ""
}
])

 */