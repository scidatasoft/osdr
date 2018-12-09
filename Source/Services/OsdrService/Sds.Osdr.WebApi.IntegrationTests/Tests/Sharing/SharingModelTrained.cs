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
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests.Tests.Sharing
{
    [Collection("OSDR Test Harness")]
    public class SharingModelTrained : OsdrWebTest
    {
        private Guid FolderId { get; set; }
        private Guid BlobId { get { return GetBlobId(FolderId); } }
        
        public SharingModelTrained(OsdrWebTestHarness fixture, ITestOutputHelper output) : base(fixture, output)
        {
            FolderId = TrainModel(JohnId.ToString(), "combined lysomotrophic.sdf", new Dictionary<string, object>() { { "parentId", JohnId }, { "case", "valid one model with success optimization" } }, true).Result;
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
    }
}