using FluentAssertions;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain;
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
    public class TrainOneModelAndFailDuringTheReportGenerationFixture
    {
        public Guid FolderId { get; set; }

        public TrainOneModelAndFailDuringTheReportGenerationFixture(OsdrTestHarness harness)
        {
            FolderId = harness.TrainModel(harness.JohnId.ToString(), "drugbank_10_records.sdf", new Dictionary<string, object>() { { "parentId", harness.JohnId }, { "case", "train one model and fail during the report generation" } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class TrainOneModelAndFailDuringTheReportGeneration : OsdrTest, IClassFixture<TrainOneModelAndFailDuringTheReportGenerationFixture>
    {
        private Guid BlobId { get { return GetBlobId(FolderId); } }
        private Guid FolderId { get; set; }

        public TrainOneModelAndFailDuringTheReportGeneration(OsdrTestHarness fixture, ITestOutputHelper output, TrainOneModelAndFailDuringTheReportGenerationFixture initFixture) : base(fixture, output)
        {
            FolderId = initFixture.FolderId;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_TrainOneModelAndFailDuringTheReportGeneration_There_Are_No_Errors()
        {
            Fixture.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_TrainOneModelAndFailDuringTheReportGeneration_WaitsWhileAllAssociatedGenericFilesProcessed()
        {
            var models = await Fixture.GetDependentFilesExcept(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);
            models.Should().HaveCount(1);

            var modelId = models.Single();

            var model = await Session.Get<Model>(modelId);
            model.Should().NotBeNull();
            model.Status.Should().Be(ModelStatus.Processed);
            model.Images.Should().HaveCount(3);
            var files = Fixture.GetDependentFiles(FolderId).ToList();
            files.Should().HaveCount(3);
            files.ToList().ForEach(async fileId =>
            {
                var file = await Session.Get<File>(fileId);
                file.Should().NotBeNull();
                file.Status.Should().Be(FileStatus.Processed);
            });

            await Task.CompletedTask;
        }
    }
}