using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Domain.BddTests.Extensions;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using Sds.Osdr.WebApi.Requests;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    public class SingleStructurePredictionsFixture
    {
        private bool _isInitialized = false;
        public Guid FolderId { get; set; }
        
        public SingleStructurePredictionsFixture()
        {
        }

        public void Initialize(Guid userId, OsdrWebTest fixture)
        {
            if (!_isInitialized)
            {
                _isInitialized = true;

                FolderId = fixture.TrainModel(userId.ToString(), "combined lysomotrophic.sdf",
                    new Dictionary<string, object>()
                    {
                        {"parentId", userId},
                        {"case", "valid one model with success optimization"}
                    }, true).Result;
            }
        }
    }
    [Collection("OSDR Test Harness")]
    public class SingleStructurePredictions : OsdrWebTest, IClassFixture<SingleStructurePredictionsFixture>
    {
        private Guid FolderId => _testFixture.FolderId;
        private Guid BlobId => GetBlobId(FolderId);
        private SingleStructurePredictionsFixture _testFixture;

        public SingleStructurePredictions(SingleStructurePredictionsFixture testFixture, OsdrWebTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            _testFixture = testFixture;
            
            _testFixture.Initialize(JohnId, this);
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.Sharing)]
        public async Task MlProcessing_ModelTraining_There_Are_No_Errors()
        {
            Fixture.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_AllGenericFilesProcessed()
        {
            var models = await Fixture.GetDependentFilesExcept(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);
            foreach (var modelId in models)
            {
                var model = await Session.Get<Model>(modelId);
                model.Should().NotBeNull();
                model.Status.Should().Be(ModelStatus.Processed);

                await JohnApi.SetPublicModelsEntity(modelId, true);
                Harness.WaitWhileModelShared(modelId);

                var responseSSP = await JohnApi.CreateSingleStructurePridiction(new RunSingleStructurePrediction
                {
                    Format = "SMILES",
                    ModelIds = new List<Guid> { modelId },
                    PropertyName = "name",
                    Structure = "C1C=CC=C1C1C=CC=C1"
                });
                var predictionId = JToken.Parse(await responseSSP.Content.ReadAsStringAsync())["predictionId"].ToObject<Guid>();
                responseSSP.EnsureSuccessStatusCode();

                var responseStatus = await JohnApi.GetPredictionStatus(predictionId);
                
                var status = JToken.Parse(await responseStatus.Content.ReadAsStringAsync());
                status["id"].ToObject<Guid>().ShouldBeEquivalentTo(predictionId);
//                status["status"].ToObject<string>().ShouldAllBeEquivalentTo("CALCULATING");
            }

            await Task.CompletedTask;
        }
    }
}