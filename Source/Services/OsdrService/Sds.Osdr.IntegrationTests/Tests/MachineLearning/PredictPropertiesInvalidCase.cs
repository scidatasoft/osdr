using FluentAssertions;
using Sds.Osdr.IntegrationTests.Traits;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Sds.Osdr.IntegrationTests
{
    public class PredictPropertiesInvalidCaseFixture
    {
        public Guid FolderId { get; set; }

        public PredictPropertiesInvalidCaseFixture(OsdrTestHarness harness)
        {
            FolderId = harness.PredictProperties("invalid case", "combined lysomotrophic.sdf", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class PredictPropertiesInvalidCase : OsdrTest, IClassFixture<PredictPropertiesInvalidCaseFixture>
    {
        private Guid BlobId { get { return GetBlobId(FolderId); } }
        private Guid FolderId { get; set; }

        public PredictPropertiesInvalidCase(OsdrTestHarness fixture, PredictPropertiesInvalidCaseFixture initFixture) : base(fixture)
        {
            FolderId = initFixture.FolderId;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlPrediction_ThereAreNoErrors()
        {
            Fixture.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }
    }
}
