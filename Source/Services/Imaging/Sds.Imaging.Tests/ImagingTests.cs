using FluentAssertions;
using Ploeh.AutoFixture.Xunit2;
using Sds.Imaging.Domain.Commands;
using Sds.Imaging.Domain.Events;
using Sds.Imaging.Domain.Models;
using Sds.MassTransit.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sds.Imaging.Tests
{
    public class ImageGeneratedEvent : ImageGenerated
    {
        public Guid Id { get; set; }
        public Guid BlobId { get; set; }
        public string Bucket { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public Guid UserId { get; set; }
        public Image Image { get; set; }
        public Guid CorrelationId { get; set; }
    }


    public class WhenUploadMolFileIntoImaging : IClassFixture<ImagingTestFixture>
    {
        private const string BUCKET = "ImagingUnitTests";
        private const string _fileName = "aspirin.mol";
        private const string _contentType = "chemical/x-mdl-molfile";
        private readonly ImagingTestFixture fixture;

        public WhenUploadMolFileIntoImaging(ImagingTestFixture fixture)
        {
            this.fixture = fixture;
        }

        [Theory, AutoData]
        public async Task Send_command_to_generate_image_from_mol_should_publish_one_ImageGenerated_event([Frozen]ImageGeneratedEvent expectedEvent)
        {
            if (expectedEvent.Image.Height < 16)
                expectedEvent.Image.Height = 16;
            if (expectedEvent.Image.Width < 16)
                expectedEvent.Image.Width = 16;

            await fixture.BlobStorage.AddFileAsync(expectedEvent.BlobId, _fileName, Resource.aspirin, _contentType, expectedEvent.Bucket);

            foreach (string format in fixture.ImageFormatsForMolFile)
            {
                try
                {
                    await fixture.Harness.Start();

                    expectedEvent.Image.Format = format;

                    await fixture.Harness.Bus.Publish<GenerateImage>(new
                    {
                        Id = expectedEvent.Id,
                        Bucket = expectedEvent.Bucket,
                        BlobId = expectedEvent.BlobId,
                        Image = expectedEvent.Image,
                        CorrelationId = expectedEvent.CorrelationId,
                        UserId = expectedEvent.UserId
                    });

                    await fixture.Harness.Published.Any<ImageGenerated>();

                    var allEvents = fixture.Harness.Published.ToList();

                    var imageGenerated = allEvents.Select<ImageGenerated>().FirstOrDefault();
                    imageGenerated.Should().NotBeNull();
                    imageGenerated.ShouldBeEquivalentTo(expectedEvent,
                       options => options
                           .Excluding(p => p.TimeStamp)
                           .Excluding(p => p.Image.MimeType)
                       );
                }
                finally
                {
                    await fixture.Harness.Stop();
                }
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_generate_svg_from_mol_should_publish_one_ImageGenerated_event(ImageGeneratedEvent expectedEvent)
        {
            expectedEvent.Image.MimeType = "image/svg+xml";
            expectedEvent.Image.Format = "svg";
            if (expectedEvent.Image.Height < 16)
                expectedEvent.Image.Height = 16;
            if (expectedEvent.Image.Width < 16)
                expectedEvent.Image.Width = 16;

            try
            {
                await fixture.Harness.Start();

                await fixture.BlobStorage.AddFileAsync(expectedEvent.BlobId, _fileName, Resource.aspirin, _contentType, expectedEvent.Bucket);

                await fixture.Harness.Bus.Publish<GenerateImage>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = expectedEvent.Bucket,
                    BlobId = expectedEvent.BlobId,
                    Image = expectedEvent.Image,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await fixture.Harness.Published.Any<ImageGenerated>();

                var allEvents = fixture.Harness.Published.ToList();

                var imageGenerated = allEvents.Select<ImageGenerated>().FirstOrDefault();
                imageGenerated.Should().NotBeNull();
                imageGenerated.ShouldBeEquivalentTo(expectedEvent,
                   options => options
                       .Excluding(p => p.TimeStamp)
                   );
            }
            finally
            {
                await fixture.Harness.Stop();
            }
        }
    }
}
