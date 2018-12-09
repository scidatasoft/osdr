using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.MassTransit.Extensions;
using Sds.Osdr.Chemicals.Domain.Aggregates;
using Sds.Osdr.Domain.BddTests;
using Sds.Osdr.BddTests.FluentAssersions;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Domain.ValueObjects;
using Sds.Osdr.RecordsFile.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Sds.Osdr.BddTests.Traits;

namespace Sds.Osdr.BddTests
{
    public partial class OsdrIntegrationTests : OsdrTest
    {
        [Fact(Skip = "No implement"), ProcessingTrait(TraitGroup.All, TraitGroup.WebPage)]
		public async Task WebPageProcessing_ChemicalWikiPage_GeneratesOneChemicalSubstance()
        {
            var startTime = DateTimeOffset.UtcNow;
            var pageId = await LoadWebPage(UserId, UserId.ToString(), "https://en.wikipedia.org/wiki/Aspirin");

            var finished = await Harness.WaitWhileAllProcessed();
            //var missed = await GetMissedMessages();
            finished.Should().BeTrue();

            var fileView = Files.Find(new BsonDocument("_id", pageId)).FirstOrDefault() as IDictionary<string, object>;
            fileView.Should().NotBeNull();

            var blob = fileView["Blob"] as IDictionary<string, object>;
            var blobInfo = await BlobStorage.GetFileInfo((Guid)blob["_id"], UserId.ToString());
            blobInfo.Should().NotBeNull();

            var fileId = fileView["_id"];
            fileId.Should().NotBeNull().And.BeOfType<Guid>();

            var file = await Session.Get<WebPage.Domain.WebPage>((Guid)fileId);
            file.Should().NotBeNull().And.ShouldBeEquivalentTo(new
            {
                Id = fileId,
                Type = FileType.WebPage,
                Bucket = UserId.ToString(),
                BlobId = pageId,
                OwnedBy = UserId,
                CreatedBy = UserId,
                CreatedDateTime = DateTimeOffset.UtcNow,
                UpdatedBy = UserId,
                UpdatedDateTime = DateTimeOffset.UtcNow,
                ParentId = UserId,
                FileName = blobInfo.FileName,
                Lenght = blobInfo.Length,
                Md5 = blobInfo.MD5,
                IsDeleted = false,
                Status = FileStatus.Processed
            }, options => options
                .ExcludingMissingMembers()
                .Using<DateTimeOffset>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 5000)).WhenTypeIs<DateTimeOffset>()
            );

            fileView.Should().WebPageEntityShouldBeEquivalentTo(file);

            var fileNode = Nodes.Find(new BsonDocument("_id", (Guid)fileId)).FirstOrDefault() as IDictionary<string, object>;
            fileNode.Should().NotBeNull().And.WebNodeShouldBeEquivalentTo(file);

            var recordView = Records.Find(new BsonDocument("FileId", (Guid)fileId)).FirstOrDefault() as IDictionary<string, object>;
            recordView.Should().NotBeNull();

            var recordBlob = recordView["Blob"];
            recordBlob.Should().NotBeNull().And.BeAssignableTo<IDictionary<string, object>>();

            var recordBlobId = (recordBlob as IDictionary<string, object>)["_id"];
            recordBlobId.Should().NotBeNull().And.BeOfType<Guid>();

            var recordId = recordView["_id"];
            recordId.Should().NotBeNull().And.BeOfType<Guid>();

            var record = await Session.Get<Substance>((Guid)recordId);
            record.Should().NotBeNull();
            record.ShouldBeEquivalentTo(new
            {
                Id = recordId,
                RecordType = RecordType.Structure,
                Bucket = UserId.ToString(),
                BlobId = recordBlobId,
                OwnedBy = UserId,
                CreatedBy = UserId,
                CreatedDateTime = startTime,
                UpdatedBy = UserId,
                UpdatedDateTime = startTime,
                ParentId = fileId,
                Status = RecordStatus.Processed,
                Index = 0,
                Issues = new List<Issue>() { new Issue { Code = "Code", AuxInfo = "AuxInfo", Message = "Message", Severity = Severity.Information, Title = "Title" } }
            }, options => options
                .ExcludingMissingMembers()
                .Using<DateTimeOffset>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 5000)).WhenTypeIs<DateTimeOffset>()
            );
            record.Images.Should().NotBeNullOrEmpty().And.ContainSingle();
            record.Fields.Should().NotBeNullOrEmpty().And.HaveCount(3);
            record.Properties.Should().NotBeNullOrEmpty().And.HaveCount(9);

            recordView.Should().EntityShouldBeEquivalentTo(record);

            var recordNode = Nodes.Find(new BsonDocument("_id", (Guid)recordId)).FirstOrDefault() as IDictionary<string, object>;
            recordNode.Should().NotBeNull().And.NodeShouldBeEquivalentTo(record);

        }
    }
}
