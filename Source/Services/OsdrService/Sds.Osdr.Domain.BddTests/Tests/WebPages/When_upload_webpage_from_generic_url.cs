using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Sds.MassTransit.Extensions;
using Sds.Osdr.Domain.BddTests;
using Sds.Osdr.BddTests.FluentAssersions;
using Sds.Osdr.Generic.Domain;
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
        public async Task WebPageProcessing_GenericWebPage_GeneratesAppropriateModels()
        {
            var startTime = DateTimeOffset.UtcNow;
            var url = "http://lifescience.opensource.epam.com/indigo/api/#loading-molecules-and-query-molecules";
            var pageId = await LoadWebPage(UserId, UserId.ToString(), url);

            var finished = await Harness.WaitWhileAllProcessed();
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
                CreatedDateTime = startTime,
                UpdatedBy = UserId,
                UpdatedDateTime = startTime,
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
        }
    }
}
