using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain.ValueObjects;
using Sds.Osdr.IntegrationTests.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.IntegrationTests
{
    public class TrainOneValidModelAndSharingFixture
    {
        public Guid FolderId { get; set; }

        public TrainOneValidModelAndSharingFixture(OsdrTestHarness harness)
        {
            FolderId = harness.TrainModel(harness.JohnId.ToString(), "combined lysomotrophic.sdf", new Dictionary<string, object>() { { "parentId", harness.JohnId }, { "case", "valid one model" } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class TrainOneModelAndSharing : OsdrTest, IClassFixture<TrainOneValidModelAndSharingFixture>
    {
        private Guid BlobId { get { return GetBlobId(FolderId); } }
        private Guid FolderId { get; set; }

        public TrainOneModelAndSharing(OsdrTestHarness fixture, ITestOutputHelper output, TrainOneValidModelAndSharingFixture initFixture) : base(fixture, output)
        {
            FolderId = initFixture.FolderId;
        }

        [Fact(Skip = "Not finished..."), ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_There_Are_No_Errors()
        {
            Fixture.GetFaults().Should().BeEmpty();

            var models = await Models.FindAsync<BsonDocument>(new BsonDocument("ParentId", FolderId));
            var model = models.ToList().First();
            var modelId = model["_id"].AsGuid;
            var permissions = new AccessPermissions
            {
                IsPublic = true,
                Users = new HashSet<Guid>(),
                Groups = new HashSet<Guid>()
            }; ;

            await Bus.Publish<MachineLearning.Domain.Commands.GrantAccess>(new
            {
                Id = modelId,
                Permissions = permissions,
                UserId = JohnId
            });
            
            Fixture.WaitWhileModelShared(modelId);

            await Task.CompletedTask;
        }

    }
}