using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.BddTests.FluentAssersions;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.BddTests
{
    [Collection("OSDR Test Harness")]
    public class UploadOneModel : OsdrTest
    {
        private Guid ModelId { get; set; }

        public UploadOneModel(OsdrTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            var modelBlobId = UploadModel(JohnId.ToString(), "Bernoulli_Naive_Bayes_with_isotonic_class_weights.sav", 
                new Dictionary<string, object>()
                { { "parentId", JohnId },
                    {"FileType", "MachineLearningModel" },
                    {"ModelInfo", new Dictionary<string, object>()
                    {
                        {"Dataset", new Dataset("title", "description")},
                        {"Property", new Property("category", "name", "units", "description") },
                        {"ModelName", "Some model name"},
                        {"Method", "NaiveBayes" },
                        {"MethodDisplayName", "Naive Bayes" },
                        {"ClassName", "Soluble" },
                        {"TestDatasetSize", 0.2 },
                        {"KFold", 4 },
                        {"Scaler", "scaler"},
                        {"Fingerprints", new List<Fingerprint>(){ new Fingerprint { Type = "ecfp", Size = 1024, Radius = 3} } }
                    } }
                }).Result;

            var modelView = Models.Find(new BsonDocument("Blob._id", modelBlobId)).FirstOrDefault() as IDictionary<string, object>;
            ModelId = modelView["_id"].As<Guid>();
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelUpload_There_Are_No_Errors()
        {
            Fixture.GetFaults().Should().BeEmpty();

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
            var model = await Fixture.Session.Get<Model>(ModelId);

            var modelNode = Nodes.Find(new BsonDocument("_id", ModelId)).FirstOrDefault() as IDictionary<string, object>;
            modelNode.Should().NotBeNull();
            modelNode.Should().ModelNodeShouldBeEquivalentTo(model);

            var modelEntity = Models.Find(new BsonDocument("_id", ModelId)).FirstOrDefault() as IDictionary<string, object>;
            modelEntity.Should().NotBeNull();
            modelEntity.Should().ModelEntityShouldBeEquivalentTo(model);
        }
    }
}