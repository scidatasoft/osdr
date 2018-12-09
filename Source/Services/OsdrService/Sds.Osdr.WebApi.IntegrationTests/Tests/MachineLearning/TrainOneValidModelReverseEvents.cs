using FluentAssertions;
using Newtonsoft.Json.Linq;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Generic.Domain;
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
    public class TrainOneValidModelReverseEvents : OsdrWebTest
    {
        private Guid FolderId { get; set; }
        private Guid BlobId { get { return GetBlobId(FolderId); } }

        public TrainOneValidModelReverseEvents(OsdrWebTestHarness fixture, ITestOutputHelper output) 
            : base(fixture, output)
        {
            FolderId = TrainModel(JohnId.ToString(), "combined lysomotrophic.sdf", new Dictionary<string, object>() { { "parentId", JohnId }, { "case", "valid one model (reverse events order)" } }).Result;
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ReverseEvnetsOrder_ThereAreNoErrors()
        {
            Fixture.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_WaitsWhileAllAssociatedGenericFilesProcessed()
        {
            var models = await Fixture.GetDependentFilesExcept(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);
            models.Should().HaveCount(1);

            foreach (var modelId in models)
            {
                var modelGenericFiles =
                    await Fixture.GetDependentFiles(modelId, FileType.Image, FileType.Tabular, FileType.Pdf);
                modelGenericFiles.Should().HaveCount(5);

                modelGenericFiles.ToList().ForEach(async fileId =>
                {
                    var file = await Session.Get<File>(modelId);
                    file.Should().NotBeNull();
                    file.Status.Should().Be(FileStatus.Processed);
                });
            }

            var reportFiles = await Fixture.GetDependentFiles(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);
            reportFiles.Should().HaveCount(3);

            reportFiles.ToList().ForEach(async id =>
            {
                var fileResponse = await JohnApi.GetFileEntityById(id);
                fileResponse.EnsureSuccessStatusCode();

                var jsonFile = JToken.Parse(await fileResponse.Content.ReadAsStringAsync());
                jsonFile["status"].Should().BeEquivalentTo("Processed");
            });

            await Task.CompletedTask;
        }
    }
}