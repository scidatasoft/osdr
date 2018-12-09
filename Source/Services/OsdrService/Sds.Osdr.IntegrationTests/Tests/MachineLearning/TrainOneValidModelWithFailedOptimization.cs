using FluentAssertions;
using Sds.Osdr.IntegrationTests.Traits;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.IntegrationTests
{
    public class TrainOneValidModelWithFailedOptimizationFixture
    {
        public Guid FolderId { get; set; }

        public TrainOneValidModelWithFailedOptimizationFixture(OsdrTestHarness harness)
        {
            FolderId = harness.TrainModel(harness.JohnId.ToString(), "combined lysomotrophic.sdf", new Dictionary<string, object>() { { "parentId", harness.JohnId }, { "case", "train model with failed optimization" } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class TrainOneValidModelWithFailedOptimization : OsdrTest, IClassFixture<TrainOneValidModelWithFailedOptimizationFixture>
    {
        private Guid BlobId { get { return GetBlobId(FolderId); } }
        private Guid FolderId { get; set; }

        public TrainOneValidModelWithFailedOptimization(OsdrTestHarness fixture, ITestOutputHelper output, TrainOneValidModelWithFailedOptimizationFixture initFixture) : base(fixture, output)
        {
            FolderId = initFixture.FolderId;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_There_Are_No_Errors()
        {
            Harness.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }
    }
}