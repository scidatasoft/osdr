using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Domain.BddTests.Extensions;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using Xunit;
using Xunit.Abstractions;
using System.Linq;
using Sds.Osdr.BddTests.FluentAssersions;

namespace Sds.Osdr.WebApi.IntegrationTests.WebPages
{
    [Collection("OSDR Test Harness")]
    public class GetNumberPage : OsdrWebTest
    {
        private Dictionary<string, Guid> FilesId { get; set; }
        private Dictionary<string, Guid> FoldersId { get; set; }
        
        public GetNumberPage(OsdrWebTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            FilesId = new Dictionary<string, Guid>();
            FoldersId = new Dictionary<string, Guid>();
            
            AddFolder("first", JohnId);
            AddFolder("second", JohnId);
            AddFiles(FoldersId["first"], new[]
            {
                "1100110.cif", "1100118.cif",
                "1100119.cif", "1100133.cif"
            });
            AddFiles(JohnId, new[]
            {
                "1100172.cif", "1100231.cif", 
                "1100252.cif", "1100331.cif"
            });
        }

        public void AddFolder(string name, Guid parentId)
        {
            var response = JohnApi.CreateFolderEntity(parentId, name).Result;
            var folderLocation = response.Headers.Location.ToString();
            var _folderId = Guid.Parse(folderLocation.Substring(folderLocation.LastIndexOf("/") + 1));

            Harness.WaitWhileFolderCreated(_folderId);
            
            FoldersId.Add(name, _folderId);
        }

        public void AddFiles(Guid parentId, IEnumerable<string> files)
        {
            foreach (var file in files)
            {    
                var fileId = ProcessRecordsFile(JohnId.ToString(), file,
                    new Dictionary<string, object>() {{"parentId", parentId}}).Result;

                var fileName = file;
                FilesId.Add(fileName, fileId);
            }
        }

        public JToken GetFileIdByName(JToken jsonFiles, string name)
        {
            JToken json = null;

            foreach (var file in jsonFiles)
            {
                if (file["name"].ToObject<string>() == name)
                    json = file;
            }

            return json;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Generic)]
        public async Task GetNumberPage_PageSize20_ReturnsNumberPageTwoInFolderFirst()
        {
            var fieldNamePageNumber = "pageNumber";
            var responseFiles = await JohnApi.GetEntitiesMe("files");
            responseFiles.EnsureSuccessStatusCode();
            var jsonFiles = JToken.Parse(await responseFiles.Content.ReadAsStringAsync());

            var jsonFileInFolderFirst = GetFileIdByName(jsonFiles, "1100119.cif");
            var jsonFileInFolderRoot = GetFileIdByName(jsonFiles, "1100252.cif");

            var fileIdInFirst = FilesId[jsonFileInFolderFirst["name"].ToObject<string>()];
            var fileIdInRoot = FilesId[jsonFileInFolderRoot["name"].ToObject<string>()];

            var response = await JohnApi.GetPageNumber(fileIdInFirst, 4);
            var numberPage1 = JToken.Parse(await response.Content.ReadAsStringAsync())[fieldNamePageNumber].ToObject<int>();
            numberPage1.ShouldBeEquivalentTo(1);
            
            var responseTwoNumberPage = await JohnApi.GetPageNumber(fileIdInFirst, 2);
            var numberPageTwoNumberPage = JToken.Parse(await responseTwoNumberPage.Content.ReadAsStringAsync())[fieldNamePageNumber].ToObject<int>();
            numberPageTwoNumberPage.ShouldBeEquivalentTo(2);

            var responseOneInRoot = await JohnApi.GetPageNumber(fileIdInRoot, 4);
            var numberPageOneInRoot = JToken.Parse(await responseOneInRoot.Content.ReadAsStringAsync())[fieldNamePageNumber].ToObject<int>();
            numberPageOneInRoot.ShouldBeEquivalentTo(2);

            var responseTwoInRoot = await JohnApi.GetPageNumber(fileIdInRoot, 2);
            var numberPageTwoInRoot = JToken.Parse(await responseTwoInRoot.Content.ReadAsStringAsync())[fieldNamePageNumber].ToObject<int>();
            numberPageTwoInRoot.ShouldBeEquivalentTo(3);
//            var response2 = await Api.GetPageNumber(FilesId[UserId][FilesId.Count - 1], 20);
//            var numberPage2 = JToken.Parse(await response2.Content.ReadAsStringAsync()).ToObject<int>();
//            numberPage2.ShouldBeEquivalentTo(1);
            
//            var response3 = await Api.GetPageNumber(FilesId[UserId][0], 20);
//            var numberPage3 = JToken.Parse(await response3.Content.ReadAsStringAsync()).ToObject<int>();
//            numberPage3.ShouldBeEquivalentTo(1);
	        
//            var paginationHeader = response.Headers.GetValues("X-Pagination").Single();
//            var jsonPagination = JObject.Parse(paginationHeader);
//            jsonPagination.Should().ContainsJson($@"
//            {{
//                'totalCount': 4,
//                'pageSize': 4,
//                'totalPages': 1,
//                'currentPage': 1
//            }}");
        }
    }
}