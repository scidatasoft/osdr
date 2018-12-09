using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Domain.BddTests.Extensions;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    public class SharingMLModelFixture
    {
        private bool _isInitialized = false;
        public Guid FolderId { get; set; }
        
        public SharingMLModelFixture()
        {
        }

        public void Initialize(Guid userId, OsdrWebTest fixture)
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                
                FolderId = fixture.TrainModel(userId.ToString(), "combined lysomotrophic.sdf", new Dictionary<string, object>() { { "parentId", userId }, { "case", "valid one model with success optimization" } }, true).Result;
            }
        }
    }

    [Collection("OSDR Test Harness")]
    public class SharingMLModel : OsdrWebTest, IClassFixture<SharingMLModelFixture>
    {
        private Guid BlobId { get { return GetBlobId(_testFixture.FolderId); } }
        private SharingMLModelFixture _testFixture;

        public SharingMLModel(SharingMLModelFixture testFixture, OsdrWebTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            _testFixture = testFixture;
            
            _testFixture.Initialize(JohnId, this);
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_There_Are_No_Errors()
        {
            Fixture.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_AllGenericFilesProcessed()
        {
            var models = await Fixture.GetDependentFilesExcept(_testFixture.FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);
            foreach (var modelId in models)
            {
                var model = await Session.Get<Model>(modelId);
                model.Should().NotBeNull();
                model.Status.Should().Be(ModelStatus.Processed);
                
                var responseSetPublic = JohnApi.SetPublicModelsEntity(modelId, true).GetAwaiter().GetResult();
                Harness.WaitWhileModelShared(modelId);
                responseSetPublic.EnsureSuccessStatusCode();
            }

            var response = await JohnApi.GetPublicNodes();
            var nodesContent = await response.Content.ReadAsStringAsync();
            var nodes = JToken.Parse(nodesContent);
            nodes.Should().HaveCount(1);
            
            
            await Task.CompletedTask;
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_AllGenericEntities()
        {
            var models = await Fixture.GetDependentFilesExcept(_testFixture.FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);
            foreach (var modelId in models)
            {
                var model = await Session.Get<Model>(modelId);
                model.Should().NotBeNull();
                model.Status.Should().Be(ModelStatus.Processed);
                
                var responseSetPublic = JohnApi.SetPublicModelsEntity(modelId, true).GetAwaiter().GetResult();
                Harness.WaitWhileModelShared(modelId);
                responseSetPublic.EnsureSuccessStatusCode();
            }

            var response = await JohnApi.GetPublicEntity("models");
            var nodesContent = await response.Content.ReadAsStringAsync();
            var entities = JToken.Parse(nodesContent);
            entities.Should().HaveCount(1);
            
            
            await Task.CompletedTask;
        }
    }
}