using FluentAssertions;
using Ploeh.AutoFixture.Xunit2;
using Sds.Imaging.Domain.Commands;
using Sds.Imaging.Domain.Events;
using Sds.Imaging.Domain.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sds.Imaging.Tests
{
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
        public async Task Send_command_to_generate_image_from_mol_should_publish_one_ImageGenerated_event([Frozen]ImageGenerated expectedEvent)
        {
            await fixture.BlobStorage.AddFileAsync(expectedEvent.BlobId, _fileName, Resource.aspirin, _contentType, BUCKET);
            foreach (string format in fixture.ImageFormatsForMolFile)
            {
                expectedEvent.Image.Format = format;

                await fixture.Harness.Bus.Publish<GenerateImage>(new GenerateImage(expectedEvent.Id, BUCKET, expectedEvent.BlobId, expectedEvent.Image, expectedEvent.CorrelationId, expectedEvent.UserId));

                //fixture.AllEvents
                //    .Should().ContainItemsAssignableTo<ImageGenerated>()
                //    .And.ContainSingle();

                //var @event = fixture.AllEvents.First();

                //@event.ShouldBeEquivalentTo(expectedEvent,
                //    options => options
                //        .Excluding(p => p.TimeStamp)
                //        .Excluding(p => p.Version)
                //    );
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_generate_svg_from_mol_should_publish_one_ImageGenerated_event(ImageGenerated expectedEvent)
        {
            expectedEvent.Image.Format = "svg";
            if (expectedEvent.Image.Height < 16)
                expectedEvent.Image.Height = 16;
            if (expectedEvent.Image.Width < 16)
                expectedEvent.Image.Width = 16;

            await fixture.BlobStorage.AddFileAsync(expectedEvent.BlobId, _fileName, Resource.aspirin, _contentType, BUCKET);

            await fixture.Harness.Bus.Publish(new GenerateImage(expectedEvent.Id, BUCKET, expectedEvent.BlobId, expectedEvent.Image, expectedEvent.CorrelationId, expectedEvent.UserId));

            //fixture.AllEvents
            //    .Should().ContainItemsAssignableTo<ImageGenerated>()
            //    .And.ContainSingle();

            //var @event = fixture.AllEvents.First();

            //@event.ShouldBeEquivalentTo(expectedEvent,
            //   options => options
            //       .Excluding(p => p.TimeStamp)
            //       .Excluding(p => p.Version)
            //   );
        }
    }
}
