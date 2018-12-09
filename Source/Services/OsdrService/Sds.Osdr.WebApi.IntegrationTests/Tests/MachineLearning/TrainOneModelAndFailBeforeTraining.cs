using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Generic.Domain;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    [Collection("OSDR Test Harness")]
    public class TrainOneModelAndFailBeforeTraining : OsdrWebTest
    {
        private Guid BlobId { get { return GetBlobId(FolderId); } }
        private Guid FolderId { get; set; }
        
        public TrainOneModelAndFailBeforeTraining(OsdrWebTestHarness fixture, ITestOutputHelper output) 
            : base(fixture, output)
        {
            FolderId = TrainModel(JohnId.ToString(), "drugbank_10_records.sdf", new Dictionary<string, object>() { { "parentId", JohnId }, { "case", "fail before starting training" } }).Result;
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_InvalidModelTraining_There_Are_No_Errors()
        {
            Fixture.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_InvalidModelTraining_DidNotGenerateAnyGenericFiles()
        {
            var models = await Fixture.GetDependentFilesExcept(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);
            models.Should().HaveCount(1);

            var files = await Fixture.GetDependentFiles(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);
            files.Should().HaveCount(0);

            await Task.CompletedTask;
        }
    }
}