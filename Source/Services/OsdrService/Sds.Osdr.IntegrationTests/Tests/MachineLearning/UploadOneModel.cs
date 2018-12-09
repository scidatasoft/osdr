using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.IntegrationTests
{
    public class UploadOneModelFixture
    {
        public Guid ModelId { get; set; }

        public UploadOneModelFixture(OsdrTestHarness harness)
        {
            ModelId = harness.UploadModel(harness.JohnId.ToString(), "Bernoulli_Naive_Bayes_with_isotonic_class_weights.sav",
                new Dictionary<string, object>()
                {
                    { "parentId", harness.JohnId },
                    {"FileType", "MachineLearningModel" },
                    {"ModelInfo", new Dictionary<string, object>()
                        {
                            //{"Dataset", new Dataset("title", "description")},
                            //{"Property", new Property("category", "name", "units", "description") },
                            {"ModelName", "Some model name"},
                            {"Method", "NaiveBayes" },
                            {"MethodDisplayName", "Naive Bayes" },
                            {"ClassName", "Soluble" },
                            {"TestDatasetSize", 0.2 },
                            {"KFold", 4 },
                            {"Scaler", "scaler"},
                            //{"Fingerprints", new List<Fingerprint>(){ new Fingerprint { Type = "ecfp", Size = 1024, Radius = 3} } }
                        }
                    }
                }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class UploadOneModel : OsdrTest, IClassFixture<UploadOneModelFixture>
    {
        private Guid ModelId { get; set; }

        public UploadOneModel(OsdrTestHarness fixture, ITestOutputHelper output, UploadOneModelFixture initFixture) : base(fixture, output)
        {
            ModelId = initFixture.ModelId;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelUpload_There_Are_No_Errors()
        {
            Harness.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelUpload_GeneratesOneModel()
        {
            var model = await Session.Get<Model>(ModelId);
            model.Should().NotBeNull();
            model.Status.Should().Be(ModelStatus.Loaded);
            model.Dataset.Should().NotBeNull();
            model.Property.Should().NotBeNull();
            model.Metadata.Should().NotBeNull();
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelUpload_ModelPersisted()
        {
            var model = await Harness.Session.Get<Model>(ModelId);

            var modelNode = Nodes.Find(new BsonDocument("_id", ModelId)).FirstOrDefault() as IDictionary<string, object>;
            modelNode.Should().NotBeNull();
            modelNode.Should().ModelNodeShouldBeEquivalentTo(model);

            var modelEntity = Models.Find(new BsonDocument("_id", ModelId)).FirstOrDefault() as IDictionary<string, object>;
            modelEntity.Should().NotBeNull();
            modelEntity.Should().ModelEntityShouldBeEquivalentTo(model);
        }
    }
}