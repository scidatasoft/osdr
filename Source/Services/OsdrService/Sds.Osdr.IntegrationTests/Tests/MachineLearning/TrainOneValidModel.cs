using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.IntegrationTests.FluentAssersions;
using Sds.Osdr.IntegrationTests.Traits;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.Pdf.Domain;
using Sds.Osdr.Tabular.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.IntegrationTests
{
    public class TrainOneValidModelFixture
    {
        public Guid FolderId { get; set; }

        public TrainOneValidModelFixture(OsdrTestHarness harness)
        {
            FolderId = harness.TrainModel(harness.JohnId.ToString(), "combined lysomotrophic.sdf", new Dictionary<string, object>() { { "parentId", harness.JohnId }, { "case", "valid one model" } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class TrainOneValidModel : OsdrTest, IClassFixture<TrainOneValidModelFixture>
    {
        private Guid BlobId { get { return GetBlobId(FolderId); } }
        private Guid FolderId { get; set; }

        public TrainOneValidModel(OsdrTestHarness fixture, ITestOutputHelper output, TrainOneValidModelFixture initFixture) : base(fixture, output)
        {
            FolderId = initFixture.FolderId;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_There_Are_No_Errors()
        {
            Harness.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_AllGenericFilesProcessed()
        {
            var models = Harness.GetDependentFilesExcept(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);
            models.Should().HaveCount(1);

            var modelId = models.Single();

            var model = await Session.Get<Model>(modelId);
            model.Should().NotBeNull();
            model.Status.Should().Be(ModelStatus.Processed);
            model.Images.Should().HaveCount(3);
            var files = Harness.GetDependentFiles(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);
            files.Should().HaveCount(3);
            models.ToList().ForEach(async fileId =>
            {
                var file = await Session.Get<File>(modelId);
                file.Should().NotBeNull();
                file.Status.Should().Be(FileStatus.Processed);
            });

            await Task.CompletedTask;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_AllGenericFilesPersisted()
        {
            var files = Harness.GetDependentFiles(FolderId).ToList();

            files.ForEach(async id =>
            {
                var file = await Session.Get<File>(id);
                var fileNode = Nodes.Find(new BsonDocument("_id", id)).FirstOrDefault() as IDictionary<string, object>;
                fileNode.Should().NotBeNull();
                fileNode.Should().NodeShouldBeEquivalentTo(file);

                var fileEntity = Files.Find(new BsonDocument("_id", id)).FirstOrDefault() as IDictionary<string, object>;
                fileEntity.Should().NotBeNull();
                fileEntity.Should().EntityShouldBeEquivalentTo(file);
            });

            await Task.CompletedTask;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_GeneratesPdfReport()
        {
            //  Check that PDF report created and has status Processed
            var modelId = Harness.GetDependentFiles(FolderId).ToList();

            modelId.First().Should().NotBe(Guid.Empty);

            var pdfFiles = Harness.GetDependentFiles(FolderId, FileType.Pdf);

            foreach (var pdfId in pdfFiles)
            {
                var file = await Harness.Session.Get<PdfFile>(pdfId);
                var entity = Files.Find(new BsonDocument("_id", pdfId)).FirstOrDefault() as IDictionary<string, object>;

                entity.Should().PdfEntityShouldBeEquivalentTo(file);
            }
            
            await Task.CompletedTask;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_GeneratesOneTabular()
        {
            var models = Harness.GetDependentFilesExcept(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);
            models.Should().HaveCount(1);

            var modelId = models.Single();
            var tabulars = Harness.GetDependentFiles(modelId, FileType.Tabular);
            tabulars.Should().NotBeNullOrEmpty();
            tabulars.Should().HaveCount(2);

            foreach (var tabularId in tabulars)
            {
                var tabular = await Harness.Session.Get<TabularFile>(tabularId);
                var tabularEntity = Files.Find(new BsonDocument("_id", tabularId)).FirstOrDefault() as IDictionary<string, object>;

                tabularEntity.Should().TabularEntityShouldBeEquivalentTo(tabular);
            }

            await Task.CompletedTask;
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_GeneratesOneModel()
        {
            var models = Harness.GetDependentFilesExcept(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);
            models.Should().HaveCount(1);

            var modelId = models.Single();

            var model = await Session.Get<Model>(modelId);
            model.Should().NotBeNull();
            model.Status.Should().Be(ModelStatus.Processed);
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_ModelProcessed()
        {
            var models = Harness.GetDependentFilesExcept(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);
            models.Should().HaveCount(1);

            var model = await Harness.Session.Get<Model>(models.First());

            model.Images.Should().HaveCount(3);
            
            model.Status.Should().Be(ModelStatus.Processed);
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlProcessing_ModelTraining_ModelPersisted()
        {
            var models = Harness.GetDependentFilesExcept(FolderId, FileType.Image, FileType.Tabular, FileType.Pdf);

            var model = await Harness.Session.Get<Model>(models.First());
            
            var modelNode = Nodes.Find(new BsonDocument("_id", model.Id)).FirstOrDefault() as IDictionary<string, object>;
            modelNode.Should().NotBeNull();
            modelNode.Should().ModelNodeShouldBeEquivalentTo(model);

            var modelEntity = Models.Find(new BsonDocument("_id", model.Id)).FirstOrDefault() as IDictionary<string, object>;
            modelEntity.Should().NotBeNull();
            modelEntity.Should().ModelEntityShouldBeEquivalentTo(model);
        }
    }
}