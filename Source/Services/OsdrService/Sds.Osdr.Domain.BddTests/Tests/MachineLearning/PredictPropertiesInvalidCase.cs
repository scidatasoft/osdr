using FluentAssertions;
using Sds.Osdr.BddTests.Traits;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Sds.Osdr.BddTests
{
    [Collection("OSDR Test Harness")]
    public class PredictPropertiesInvalidCase : OsdrTest
    {
        private Guid BlobId { get { return GetBlobId(FolderId); } }
        private Guid FolderId { get; set; }

        public PredictPropertiesInvalidCase(OsdrTestHarness fixture) : base(fixture)
        {
            FolderId = PredictProperties("invalid case", "combined lysomotrophic.sdf", new Dictionary<string, object>() { { "parentId", JohnId } }).Result;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlPrediction_ThereAreNoErrors()
        {
            Fixture.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }
    }
}
