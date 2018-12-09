using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    [Collection("OSDR Test Harness")]
    public class TrainOneModelAndFailDuringTheTraining : OsdrWebTest
    {
        private Guid BlobId { get { return GetBlobId(FolderId); } }
        private Guid FolderId { get; set; }
        
        public TrainOneModelAndFailDuringTheTraining(OsdrWebTestHarness fixture, ITestOutputHelper output) 
            : base(fixture, output)
        {
            FolderId = TrainModel(JohnId.ToString(), "drugbank_10_records.sdf", new Dictionary<string, object>() { { "parentId", JohnId }, { "case", "train one model and fail during the training" } }).Result;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_InvalidModelTraining_There_Are_No_Errors()
        {
            Fixture.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_InvalidModelTraining_WaitsWhileAllAssociatedGenericFilesProcessed()
        {
            var modelId = await Fixture.GetDependentFilesExcept(FolderId, FileType.Image, FileType.Pdf, FileType.Tabular);
            modelId.Should().HaveCount(1);
            modelId.SingleOrDefault().Should().NotBeEmpty();

            var dependentFiles = Fixture.GetDependentFiles(modelId.Single()).ToList();
            dependentFiles.Should().HaveCount(2);

            dependentFiles.ToList().ForEach(async id =>
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