using FluentAssertions;
using MongoDB.Driver;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.MachineLearning.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.BddTests
{
    [Collection("OSDR Test Harness")]
    public class TrainOneModelAndFailDuringTheTraining : OsdrTest
    {
        private Guid BlobId { get { return GetBlobId(FolderId); } }
        private Guid FolderId { get; set; }
        
        public TrainOneModelAndFailDuringTheTraining(OsdrTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            FolderId = TrainModel(JohnId.ToString(), "drugbank_10_records.sdf", new Dictionary<string, object>() { { "parentId", JohnId }, { "case", "train one model and fail during the training" } }).Result;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_InvalidModelTraining_There_Are_No_Errors()
        {
            Fixture.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_InvalidModelTraining_WaitsWhileAllAssociatedGenericFilesProcessed()
        {
            var models = Fixture.GetDependentFiles(FolderId).ToList();
            models.Should().HaveCount(1);

            var modelId = models.Single();

            var model = await Session.Get<Model>(modelId);
            model.Should().NotBeNull();
            model.Status.Should().Be(ModelStatus.Failed);
            model.Images.Should().HaveCount(0);
            var files = Fixture.GetDependentFiles(modelId).ToList();
            files.Should().HaveCount(2);
            models.ForEach(async fileId =>
            {
                var file = await Session.Get<File>(fileId);
                file.Should().NotBeNull();
                file.Status.Should().Be(FileStatus.Processed);
            });

            await Task.CompletedTask;
        }
    }
}