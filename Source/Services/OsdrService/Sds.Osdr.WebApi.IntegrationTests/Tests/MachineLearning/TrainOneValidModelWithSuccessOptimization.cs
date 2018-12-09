using FluentAssertions;
using Newtonsoft.Json.Linq;
using Sds.Osdr.BddTests.FluentAssersions;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    [Collection("OSDR Test Harness")]
    public class TrainOneValidModelWithSuccessOptimization : OsdrWebTest
    {
        private Guid FolderId { get; set; }
        private Guid BlobId { get { return GetBlobId(FolderId); } }

        public TrainOneValidModelWithSuccessOptimization(OsdrWebTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            FolderId = TrainModel(JohnId.ToString(), "combined lysomotrophic.sdf", new Dictionary<string, object>() { { "parentId", JohnId }, { "case", "valid one model with success optimization" } }, true).Result;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_There_Are_No_Errors()
        {
            Fixture.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_AllGenericFilesProcessed()
        {
            var models = await Fixture.GetDependentFilesExcept(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);
            models.Should().HaveCount(1);

            foreach (var modelId in models)
            {
                var model = await Session.Get<Model>(modelId);
                model.Should().NotBeNull();
                model.Status.Should().Be(ModelStatus.Processed);
                model.Images.Should().HaveCount(3);

                var files = Fixture.GetDependentFiles(modelId);
                files.Should().HaveCount(5);
                models.ToList().ForEach(async fileId =>
                {
                    var file = await Session.Get<File>(modelId);
                    file.Should().NotBeNull();
                    file.Status.Should().Be(FileStatus.Processed);
                });
            }

            var reportFiles = await Fixture.GetDependentFiles(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);
            reportFiles.Should().HaveCount(3);

            foreach (var fileId in reportFiles)
            {
                var model = await Session.Get<File>(fileId);
                model.Should().NotBeNull();
                model.Status.Should().Be(FileStatus.Processed);
            }

            await Task.CompletedTask;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_AllGenericFilesPersisted()
        {
            var files = await Fixture.GetDependentFiles(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);

            files.ToList().ForEach(id =>
            {
                var fileResponse = JohnApi.GetFileEntityById(id).Result;
                var fileJson = JToken.Parse(fileResponse.Content.ReadAsStringAsync().Result);
                var fileNodeResponse = JohnApi.GetNodeById(id).Result;
                fileNodeResponse.EnsureSuccessStatusCode();
                fileJson["status"].ToObject<string>().Should().BeEquivalentTo("Processed");

                var jsonNode = JToken.Parse(fileNodeResponse.Content.ReadAsStringAsync().Result);
                jsonNode.Should().NotBeEmpty();
                jsonNode.Should().ContainsJson($@"
                {{
                    'id': '{fileJson["id"].ToObject<Guid>()}',
                    'type': 'File',
                    'subType': *EXIST*,
                    'blob': *EXIST*,
                    'status': '{fileJson["status"].ToObject<string>()}',
                    'ownedBy': '{JohnId}',
                    'createdBy': '{JohnId}',
                    'createdDateTime': *EXIST*,
                    'updatedBy': '{JohnId}',
                    'updatedDateTime': *EXIST*,
                    'name': '{fileJson["name"].ToObject<string>()}',
                    'parentId': '{fileJson["parentId"].ToObject<Guid>()}',
                    'version': {fileJson["version"].ToObject<int>()}
                }}");

                if (fileJson.Contains("images") && jsonNode.Contains("images"))
                {
                    jsonNode["images"].Should().BeEquivalentTo(fileJson["images"]);
                }
            });

            await Task.CompletedTask;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_GeneratesPdfReport()
        {
            var pdfFiles = await Fixture.GetDependentFiles(FolderId, FileType.Pdf);
            pdfFiles.Should().NotBeNullOrEmpty();
            pdfFiles.Should().HaveCount(1);

            foreach (var pdfId in pdfFiles)
            {
                var pdfResponse = await JohnApi.GetFileEntityById(pdfId);
                var pdfJson = JToken.Parse(await pdfResponse.Content.ReadAsStringAsync());
                pdfJson.Should().ContainsJson($@"
                {{
                    'id': '{pdfId}',
                    'blob': *EXIST*,
                    'subType': 'Pdf',
                    'ownedBy': '{JohnId}',
                    'createdBy': '{JohnId}',
                    'createdDateTime': *EXIST*,
                    'updatedBy': '{JohnId}',
                    'updatedDateTime': *EXIST*,
                    'parentId': '{FolderId}',
                    'name': 'ML_report.pdf',
                    'status': 'Processed',
                    'version': 7
                }}");
                pdfJson["images"].Should().HaveCount(3);

                var nodePdfResponse = await JohnApi.GetNodeById(pdfId);
                var nodeJson = JToken.Parse(await nodePdfResponse.Content.ReadAsStringAsync());
                nodeJson.Should().ContainsJson($@"
                {{
                    'id': '{pdfId}',
                    'blob': *EXIST*,
                    'subType': 'Pdf',
                    'ownedBy': '{JohnId}',
                    'createdBy': '{JohnId}',
                    'createdDateTime': *EXIST*,
                    'updatedBy': '{JohnId}',
                    'updatedDateTime': *EXIST*,
                    'parentId': '{FolderId}',
                    'name': 'ML_report.pdf',
                    'status': 'Processed',
                    'version': 7
                }}");
            }

            await Task.CompletedTask;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_GeneratesOneTabular()
        {
            var tabulars = await Fixture.GetDependentFiles(FolderId, FileType.Tabular);
            tabulars.Should().NotBeNullOrEmpty();
            tabulars.Should().HaveCount(1);

            foreach (var tabularId in tabulars)
            {
                var tabularResponse = await JohnApi.GetFileEntityById(tabularId);
                var tabularJson = JToken.Parse(await tabularResponse.Content.ReadAsStringAsync());

                tabularJson.Should().ContainsJson($@"
                {{
                    'id': '{tabularId}',
                    'blob': *EXIST*,
                    'subType': 'Tabular',
                    'ownedBy': '{JohnId}',
                    'createdBy': '{JohnId}',
                    'createdDateTime': *EXIST*,
                    'updatedBy': '{JohnId}',
                    'updatedDateTime': *EXIST*,
                    'parentId': '{FolderId}',
                    'name': 'FocusSynthesis_InStock.csv',
                    'status': 'Processed',
                    'version': 7
                }}");
            }

            await Task.CompletedTask;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_GeneratesOneModel()
        {
            var models = await Fixture.GetDependentFilesExcept(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);

            models.Should().HaveCount(1);
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_ModelProcessed()
        {
            var models = await Fixture.GetDependentFilesExcept(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);

            var modelResponse = await JohnApi.GetModelEntityById(models.First());
            modelResponse.EnsureSuccessStatusCode();
            var modelJson = JToken.Parse(await modelResponse.Content.ReadAsStringAsync());

            modelJson["status"].ToObject<string>().ShouldBeEquivalentTo("Processed");
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_ModelPersisted()
        {
            var models = await Fixture.GetDependentFilesExcept(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);

            var modelResponse = await JohnApi.GetModelEntityById(models.First());
            modelResponse.EnsureSuccessStatusCode();
            var modelJson = JToken.Parse(await modelResponse.Content.ReadAsStringAsync());
            modelJson.Should().ContainsJson($@"
            {{
                'id': '{modelJson["id"].ToObject<Guid>()}',
                'blob': *EXIST*,
                'ownedBy': '{JohnId}',
                'createdBy': '{JohnId}',
                'createdDateTime': *EXIST*,
                'updatedBy': '{JohnId}',
                'updatedDateTime': *EXIST*,
                'parentId': '{FolderId}',
                'name': 'Naive Bayes',
                'status': 'Processed',
                'version': 10,
                'method': 'NaiveBayes',
                'className': 'Soluble',
                'subSampleSize': 1.0,
                'kFold': 4,
                'fingerprints': [
                {{
                    'type': 'ecfp',
                    'size': 512,
                    'radius': 2
                }}],
                'images': *EXIST*
                
            }}");

            var modelNodeResponse = await JohnApi.GetNodeById(models.First());
            modelNodeResponse.EnsureSuccessStatusCode();
            var nodeJson = JToken.Parse(await modelNodeResponse.Content.ReadAsStringAsync());

            nodeJson.Should().ContainsJson($@"
            {{
                'id': '{modelJson["id"].ToObject<Guid>()}',
                'blob': *EXIST*,
                'type': 'Model',
                'ownedBy': '{JohnId}',
                'createdBy': '{JohnId}',
                'createdDateTime': *EXIST*,
                'updatedBy': '{JohnId}',
                'updatedDateTime': *EXIST*,
                'parentId': '{FolderId}',
                'name': 'Naive Bayes',
                'status': 'Processed',
                'version': 10,
                'images': *EXIST*
            }}");
        }
    }
}