using FluentAssertions;
using MongoDB.Driver;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Generic.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.BddTests
{
    [Collection("OSDR Test Harness")]
    public class TrainOneValidModelReverseEvents : OsdrTest
    {
        private Guid BlobId { get { return GetBlobId(FolderId); } }
        private Guid FolderId { get; set; }
        
        public TrainOneValidModelReverseEvents(OsdrTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            FolderId = TrainModel(JohnId.ToString(), "combined lysomotrophic.sdf", new Dictionary<string, object>() { { "parentId", JohnId }, { "case", "valid one model (reverse events order)" } }).Result;
        }
        
        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task OneModelTraining_ReverseEventsOrder_ThereAreNoErrors()
        {
            Fixture.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }
        
        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_WaitsWhileAllAssociatedGenericFilesProcessed()
        {
            var models = await Fixture.GetDependentFilesExcept(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);
            models.Should().HaveCount(1);

            var modelId = models.Single();
            var modelGenericFiles = await Fixture.GetDependentFiles(modelId, FileType.Image, FileType.Tabular, FileType.Pdf);
            modelGenericFiles.Should().HaveCount(5);

            modelGenericFiles.ToList().ForEach(async fileId =>
            {
                var file = await Session.Get<File>(modelId);
                file.Should().NotBeNull();
                file.Status.Should().Be(FileStatus.Processed);
            });

            var reportFiles = await Fixture.GetDependentFiles(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);
            reportFiles.Should().HaveCount(3);

            reportFiles.ToList().ForEach(async id =>
            {
                var file = await Session.Get<File>(id);
                file.Should().NotBeNull();
                file.Status.Should().Be(FileStatus.Processed);
            });

            await Task.CompletedTask;
        }

        
    }
}