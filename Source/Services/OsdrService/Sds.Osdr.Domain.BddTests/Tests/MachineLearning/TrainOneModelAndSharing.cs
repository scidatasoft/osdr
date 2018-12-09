using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Domain.BddTests.Extensions;
using Sds.Osdr.Generic.Domain.ValueObjects;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.BddTests
{
    [Collection("OSDR Test Harness")]
    public class TrainOneModelAndSharing : OsdrTest
    {
        private Guid BlobId { get { return GetBlobId(FolderId); } }
        private Guid FolderId { get; set; }

        public TrainOneModelAndSharing(OsdrTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            FolderId = TrainModel(JohnId.ToString(), "combined lysomotrophic.sdf", new Dictionary<string, object>() { { "parentId", JohnId }, { "case", "valid one model" } }).Result;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
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
            
            Harness.WaitWhileModelShared(modelId);

            await Task.CompletedTask;
        }

    }
}