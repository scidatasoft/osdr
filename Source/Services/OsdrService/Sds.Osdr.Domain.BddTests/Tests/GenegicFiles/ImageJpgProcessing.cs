using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.BddTests.FluentAssersions;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Generic.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.BddTests
{
    [Collection("OSDR Test Harness")]
    public class ImageJpgProcessing : OsdrTest, IInitializerTest
    {
        private Guid FileId => Container.Enviroment.Get("FileId").AsGuid;
        private Guid BlobId => GetBlobId(FileId);
        
        public ImageJpgProcessing(ContainerTest containerTest, OsdrTestHarness fixture,
            ITestOutputHelper output) : base(fixture, output, containerTest)
        {
            Container.SetTest(this);
        }

        public void Initialize(VariablesRepository env)
        {
            var fileId = ProcessFile(JohnId.ToString(), "computer-humor-computer-science.jpg",
                new Dictionary<string, object>() {{"parentId", JohnId}}).Result;

            env.Push("FileId", fileId);
        }

        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Image)]
        public async Task ImageProcessing_ValidJpg_GeneratesAppropriateModels()
        {
            var blobInfo = await BlobStorage.GetFileInfo(BlobId, JohnId.ToString());
            blobInfo.Should().NotBeNull();

            var file = await Session.Get<File>(FileId);
            file.Should().NotBeNull();
            file.ShouldBeEquivalentTo(new
                {
                    Id = FileId,
                    Type = FileType.Image,
                    Bucket = JohnId.ToString(),
                    BlobId = BlobId,
                    PdfBucket = file.Bucket,
                    OwnedBy = JohnId,
                    CreatedBy = JohnId,
                    CreatedDateTime = DateTimeOffset.UtcNow,
                    UpdatedBy = JohnId,
                    UpdatedDateTime = DateTimeOffset.UtcNow,
                    ParentId = JohnId,
                    FileName = blobInfo.FileName,
                    Length = blobInfo.Length,
                    Md5 = blobInfo.MD5,
                    IsDeleted = false,
                    Status = FileStatus.Processed
                }, options => options
                    .ExcludingMissingMembers()
            );
            file.Images.Should().NotBeNullOrEmpty();
            file.Images.Should().HaveCount(3);
        }
        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Image)]
        public async Task ImageProcessing_ValidJpg_ExpectedFileEntity()
        {
            var file = await Session.Get<File>(FileId);
            var fileView = Files.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;

            fileView.Should().EntityShouldBeEquivalentTo(file);
        }
        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Image)]
        public async Task ImageProcessing_ValidJpg_ExpectedFileNode()
        {
            var file = await Session.Get<File>(FileId);
            var fileNode = Nodes.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;

            fileNode.Should().NodeShouldBeEquivalentTo(file);
        }

    }
}