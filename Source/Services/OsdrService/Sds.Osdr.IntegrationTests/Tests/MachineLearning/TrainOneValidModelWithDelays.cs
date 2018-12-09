using FluentAssertions;
using Sds.Osdr.IntegrationTests.Traits;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.IntegrationTests
{
    public class TrainOneValidModelWithDelaysFixture
    {
        public Guid FolderId { get; set; }

        public TrainOneValidModelWithDelaysFixture(OsdrTestHarness harness)
        {
            FolderId = harness.TrainModel(harness.JohnId.ToString(), "combined lysomotrophic.sdf", new Dictionary<string, object>() { { "parentId", harness.JohnId }, { "case", "valid one model (with delays)" } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class TrainOneValidModelWithDelays : OsdrTest, IClassFixture<TrainOneValidModelWithDelaysFixture>
    {
        private Guid BlobId { get { return GetBlobId(FolderId); } }
        private Guid FolderId { get; set; }
        
        public TrainOneValidModelWithDelays(OsdrTestHarness fixture, ITestOutputHelper output, TrainOneValidModelWithDelaysFixture initFixture) : base(fixture, output)
        {
            FolderId = initFixture.FolderId;
        }
        
        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTrainingWithDelays_ThereAreNoErrors()
        {
            Harness.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }
    }
}