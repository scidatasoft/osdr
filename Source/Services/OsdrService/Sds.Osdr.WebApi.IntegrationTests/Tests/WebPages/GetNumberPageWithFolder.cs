using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Domain.BddTests.Extensions;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests.WebPages
{
    [Collection("OSDR Test Harness")]
    public class GetNumberPageWithFolder : OsdrWebTest
    {
        public Dictionary<string, Folder> Folders { get; set; }
        
        public GetNumberPageWithFolder(OsdrWebTestHarness fixture, ITestOutputHelper output = null) : base(fixture, output)
        {
            Folders = new Dictionary<string, Folder>();
            
            Folders.Add("Root", new Folder(JohnId, JohnId, "Root", new Dictionary<string, Guid>()));
            
            AddFolder("First", JohnId);
            AddFolder("Second", JohnId);
            
            AddFiles(Folders["Root"], new []
            {
                "1100110.cif", "1100110_modified.cif", "1100118.cif", "1100331.cif"//    "ml-training-image.png", "ringcount_0.mol", "test_solubility.sdf", "wikiAspirin.mol"
            });
            AddFiles(Folders["First"], new[]
            {
                "Aspirin.mol", "chemspider.mol", "chemspider2.mol", "wikiAspirin.mol"//, "Chemical-diagram.png", "drugbank_10_records.sdf", "1100110.cif"
            });
            AddFiles(Folders["Second"], new[] 
            { 
                "1100110.cif", "1100118.cif", "1100110_modified.cif", "1100331.cif"//    "ml-training-image.png", "ringcount_0.mol", "test_solubility.sdf", "wikiAspirin.mol"
            });
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Generic)]
        public async Task GetNumberPage_FolderRoot_ReturnsNumberPage3()
        {
            var response1 = await JohnApi.GetPageNumber(Folders["Root"].Files["1100118.cif"], 2);
            var numberPage1 = JToken.Parse(await response1.Content.ReadAsStringAsync())["pageNumber"].ToObject<int>();
            numberPage1.ShouldBeEquivalentTo(3);
            
            var response2 = await JohnApi.GetPageNumber(Folders["Root"].Files["1100331.cif"], 2);
            var numberPage2 = JToken.Parse(await response2.Content.ReadAsStringAsync())["pageNumber"].ToObject<int>();
            numberPage2.ShouldBeEquivalentTo(3);
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Generic)]
        public async Task GetNumberPage_FolderRoot_ReturnsNumberPage2()
        {
            var response1 = await JohnApi.GetPageNumber(Folders["Root"].Files["1100110.cif"], 2);
            var numberPage1 = JToken.Parse(await response1.Content.ReadAsStringAsync())["pageNumber"].ToObject<int>();
            numberPage1.ShouldBeEquivalentTo(2);
            
            var response2 = await JohnApi.GetPageNumber(Folders["Root"].Files["1100110_modified.cif"], 2);
            var numberPage2 = JToken.Parse(await response2.Content.ReadAsStringAsync())["pageNumber"].ToObject<int>();
            numberPage2.ShouldBeEquivalentTo(2);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Generic)]
        public async Task GetNumberPage_FolderFirst_ReturnsNumberPage1()
        {
            var response1 = await JohnApi.GetPageNumber(Folders["First"].Files["Aspirin.mol"], 2);
            var numberPage1 = JToken.Parse(await response1.Content.ReadAsStringAsync())["pageNumber"].ToObject<int>();
            numberPage1.ShouldBeEquivalentTo(1);
            
            var response2 = await JohnApi.GetPageNumber(Folders["First"].Files["chemspider.mol"], 2);
            var numberPage2 = JToken.Parse(await response2.Content.ReadAsStringAsync())["pageNumber"].ToObject<int>();
            numberPage2.ShouldBeEquivalentTo(1);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Generic)]
        public async Task GetNumberPage_FolderFirst_ReturnsNumberPage2()
        {
            var response1 = await JohnApi.GetPageNumber(Folders["First"].Files["wikiAspirin.mol"], 2);
            var numberPage1 = JToken.Parse(await response1.Content.ReadAsStringAsync())["pageNumber"].ToObject<int>();
            numberPage1.ShouldBeEquivalentTo(2);
            
            var response2 = await JohnApi.GetPageNumber(Folders["First"].Files["chemspider2.mol"], 2);
            var numberPage2 = JToken.Parse(await response2.Content.ReadAsStringAsync())["pageNumber"].ToObject<int>();
            numberPage2.ShouldBeEquivalentTo(2);
        }

        private void AddFiles(Folder folder, string[] files)
        {
            foreach (var fileName in files)
            {
                var fileId = ProcessRecordsFile(JohnId.ToString(), fileName,
                    new Dictionary<string, object>() {{"parentId", folder.Id}}).Result;
                
                folder.AddFile(fileName, fileId);
            }

        //    FilesId[parentId].Add(fileId);
        }

        private void AddFolder(string name, Guid parentId)
        {
            var response = JohnApi.CreateFolderEntity(parentId, name).Result;
            var folderLocation = response.Headers.Location.ToString();
            var folderId = Guid.Parse(folderLocation.Substring(folderLocation.LastIndexOf("/") + 1));

            Harness.WaitWhileFolderCreated(folderId);
            
            Folders.Add(name, new Folder(folderId, parentId, name, new Dictionary<string, Guid>()));
        }

        public class Folder
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public Dictionary<string, Guid> Files { get; set; }
            public Guid ParentId { get; set; }

            public Folder(Guid id, Guid parentId, string name, Dictionary<string, Guid> files)
            {
                Id = id;
                Name = name;
                Files = files;
                ParentId = parentId;
            }

            public void AddFile(string name, Guid id)
            {
                Files.Add(name, id);
            }
        }
    }
}