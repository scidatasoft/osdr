using FluentAssertions;
using MongoDB.Driver;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.MachineLearning.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.IntegrationTests
{
    public class TrainOneModelAndFailBeforeTrainingFixture
    {
        public Guid FolderId { get; set; }

        public TrainOneModelAndFailBeforeTrainingFixture(OsdrTestHarness harness)
        {
            FolderId = harness.TrainModel(harness.JohnId.ToString(), "drugbank_10_records.sdf", new Dictionary<string, object>() { { "parentId", harness.JohnId }, { "case", "fail before starting training" } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class TrainOneModelAndFailBeforeTraining : OsdrTest, IClassFixture<TrainOneModelAndFailBeforeTrainingFixture>
    {
        private Guid BlobId { get { return GetBlobId(FolderId); } }
        private Guid FolderId { get; set; }
        
        public TrainOneModelAndFailBeforeTraining(OsdrTestHarness fixture, ITestOutputHelper output, TrainOneModelAndFailBeforeTrainingFixture initFixture) : base(fixture, output)
        {
            FolderId = initFixture.FolderId;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_InvalidModelTraining_There_Are_No_Errors()
        {
            Fixture.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_InvalidModelTraining_DidNotGenerateAnyGenericFiles()
        {
            var models = Fixture.GetDependentFiles(FolderId).ToList();
            models.Should().HaveCount(1);

            var modelId = models.Single();

            var model = await Session.Get<Model>(modelId);
            model.Should().NotBeNull();
            model.Status.Should().Be(ModelStatus.Failed);

            var files = Fixture.GetDependentFiles(modelId).ToList();
            files.Should().HaveCount(0);


            await Task.CompletedTask;
        }
    }
}