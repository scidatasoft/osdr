using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    [Collection("OSDR Test Harness")]
    public class TrainOneModelAndFailDuringTheReportGeneration : OsdrWebTest
    {
        private Guid BlobId { get { return GetBlobId(FolderId); } }
        private Guid FolderId { get; set; }
        
        public TrainOneModelAndFailDuringTheReportGeneration(OsdrWebTestHarness fixture, ITestOutputHelper output) 
            : base(fixture, output)
        {
            FolderId = TrainModel(JohnId.ToString(), "drugbank_10_records.sdf", new Dictionary<string, object>() { { "parentId", JohnId }, { "case", "train one model and fail during the report generation" } }).Result;
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_TrainOneModelAndFailDuringTheReportGeneration_There_Are_No_Errors()
        {
            Fixture.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_TrainOneModelAndFailDuringTheReportGeneration_WaitsWhileAllAssociatedGenericFilesProcessed()
        {
            var models = await Fixture.GetDependentFilesExcept(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);
            models.Should().HaveCount(1);

            foreach (var modelId in models)
            {
                var model = await Session.Get<Model>(modelId);
                model.Should().NotBeNull();
                model.Status.Should().Be(ModelStatus.Processed);
                model.Images.Should().HaveCount(3);
            }

            var files = (await Fixture.GetDependentFiles(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf)).ToList();
            files.Should().HaveCount(2);
            files.ToList().ForEach(async fileId =>
            {
                var fileResponse = await JohnApi.GetFileEntityById(fileId);
                fileResponse.EnsureSuccessStatusCode();

                var jsonFile = JToken.Parse(await fileResponse.Content.ReadAsStringAsync());
                jsonFile["status"].Should().BeEquivalentTo("Processed");
            });

            await Task.CompletedTask;


            //var files = Fixture.GetDependentFiles(FolderId).ToList();
            //files.Should().HaveCount(5);

            //files.ForEach(async id =>
            //{
            //    var fileResponse = await Api.GetFileEntityById(id);
            //    fileResponse.EnsureSuccessStatusCode();

            //    var jsonFile = JToken.Parse(await fileResponse.Content.ReadAsStringAsync());
            //    jsonFile["status"].Should().BeEquivalentTo("Processed");
            //});

            //await Task.CompletedTask;
        }
    }
}