using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Sds.Osdr.BddTests;
using Sds.Osdr.BddTests.Traits;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    [Collection("OSDR Test Harness")]
    public class TrainOneValidModelWithDelays : OsdrWebTest
    {
        private Guid BlobId { get { return GetBlobId(FolderId); } }
        private Guid FolderId { get; set; }
        
        public TrainOneValidModelWithDelays(OsdrWebTestHarness fixture, ITestOutputHelper output) 
            : base(fixture, output)
        {
            FolderId = TrainModel(JohnId.ToString(), "combined lysomotrophic.sdf", new Dictionary<string, object>() { { "parentId", JohnId }, { "case", "valid one model (with delays)" } }).Result;
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTrainingWithDelays_ThereAreNoErrors()
        {
            Fixture.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }
    }
}