using FluentAssertions;
using Ploeh.AutoFixture.Xunit2;
using Sds.ChemicalFileParser.Tests;
using Sds.PdfProcessor.Domain.Commands;
using Sds.PdfProcessor.Domain.Events;
using Sds.Storage.Blob.Core;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sds.PdfProcessor.Tests
{
    public class ValidPdfParseTests : IClassFixture<ParseFileTestFixture>
    {
        private const string BUCKET = "UnitTests";

        private readonly ParseFileTestFixture fixture;

        public ValidPdfParseTests(ParseFileTestFixture fixture)
        {
            this.fixture = fixture;
        }

        [Theory, AutoData]
        public async Task Send_command_with_mask_7_should_to_extract_whole_data_and_publish_one_FileParsed_one_TextExported_and_4ImageExported_events([Frozen]FileParsed expectedFileParsedEvent)
        {
            var blobId = await fixture.BlobStorage.AddFileAsync("bph12781.pdf", Resource.bph12781, "application/pdf", BUCKET);

            fixture.ClearAllEvents();

            await fixture.Bus.Send(new ParseFile(expectedFileParsedEvent.Id, expectedFileParsedEvent.CorrelationId, expectedFileParsedEvent.UserId, BUCKET, blobId, 7));

            fixture.AllEvents.Count().ShouldBeEquivalentTo(6);
            fixture.AllEvents.Count(e => e is FileParseFailed).ShouldBeEquivalentTo(0);
            fixture.AllEvents.Count(e => e is ImageExported).ShouldBeEquivalentTo(4);
            fixture.AllEvents.Count(e => e is TextExported).ShouldBeEquivalentTo(1);
            fixture.AllEvents.Count(e => e is FileParsed).ShouldBeEquivalentTo(1);
            //tables must be here too


            var @event = fixture.AllEvents.Last();
            expectedFileParsedEvent.ByteTypes = 7;

            @event.ShouldBeEquivalentTo(expectedFileParsedEvent,
                options => options
                    .Excluding(p => p.TimeStamp)
                    .Excluding(p => p.Version)
                );
        }

        [Theory, AutoData]
        public async Task Send_command_with_mask_6_should_to_publish_one_FileParsed_4ImageExported_events([Frozen]FileParsed expectedFileParsedEvent)
        {
            var blobId = await fixture.BlobStorage.AddFileAsync("bph12781.pdf", Resource.bph12781, "application/pdf", BUCKET);

            fixture.ClearAllEvents();

            await fixture.Bus.Send(new ParseFile(expectedFileParsedEvent.Id, expectedFileParsedEvent.CorrelationId, expectedFileParsedEvent.UserId, BUCKET, blobId, 6));

            fixture.AllEvents.Count().ShouldBeEquivalentTo(5);
            fixture.AllEvents.Count(e => e is FileParseFailed).ShouldBeEquivalentTo(0);
            fixture.AllEvents.Count(e => e is ImageExported).ShouldBeEquivalentTo(4);
            fixture.AllEvents.Count(e => e is TextExported).ShouldBeEquivalentTo(0);
            fixture.AllEvents.Count(e => e is FileParsed).ShouldBeEquivalentTo(1);

            var @event = fixture.AllEvents.Last();
            expectedFileParsedEvent.ByteTypes = 6;

            @event.ShouldBeEquivalentTo(expectedFileParsedEvent,
                options => options
                    .Excluding(p => p.TimeStamp)
                    .Excluding(p => p.Version)
                );
        }

        [Theory, AutoData]
        public async Task Send_command_with_mask_5_should_to_publish_one_FileParsed_one_TextExported_event([Frozen]FileParsed expectedFileParsedEvent)
        {
            var blobId = await fixture.BlobStorage.AddFileAsync("bph12781.pdf", Resource.bph12781, "application/pdf", BUCKET);

            fixture.ClearAllEvents();

            await fixture.Bus.Send(new ParseFile(expectedFileParsedEvent.Id, expectedFileParsedEvent.CorrelationId, expectedFileParsedEvent.UserId, BUCKET, blobId, 5));

            fixture.AllEvents.Count().ShouldBeEquivalentTo(2);
            fixture.AllEvents.Count(e => e is FileParseFailed).ShouldBeEquivalentTo(0);
            fixture.AllEvents.Count(e => e is ImageExported).ShouldBeEquivalentTo(0);
            fixture.AllEvents.Count(e => e is TextExported).ShouldBeEquivalentTo(1);
            fixture.AllEvents.Count(e => e is FileParsed).ShouldBeEquivalentTo(1);
            //tables must be here

            var @event = fixture.AllEvents.Last();
            expectedFileParsedEvent.ByteTypes = 5;

            @event.ShouldBeEquivalentTo(expectedFileParsedEvent,
                options => options
                    .Excluding(p => p.TimeStamp)
                    .Excluding(p => p.Version)
                );
        }

        [Theory, AutoData]
        public async Task Send_command_with_mask_4_should_to_publish_one_FileParsed_event([Frozen]FileParsed expectedFileParsedEvent)
        {
            var blobId = await fixture.BlobStorage.AddFileAsync("bph12781.pdf", Resource.bph12781, "application/pdf", BUCKET);

            fixture.ClearAllEvents();

            await fixture.Bus.Send(new ParseFile(expectedFileParsedEvent.Id, expectedFileParsedEvent.CorrelationId, expectedFileParsedEvent.UserId, BUCKET, blobId, 4));

            fixture.AllEvents.Count().ShouldBeEquivalentTo(1);
            fixture.AllEvents.Count(e => e is FileParseFailed).ShouldBeEquivalentTo(0);
            fixture.AllEvents.Count(e => e is ImageExported).ShouldBeEquivalentTo(0);
            fixture.AllEvents.Count(e => e is TextExported).ShouldBeEquivalentTo(0);
            fixture.AllEvents.Count(e => e is FileParsed).ShouldBeEquivalentTo(1);
            //tables must be here

            var @event = fixture.AllEvents.Last();
            expectedFileParsedEvent.ByteTypes = 4;

            @event.ShouldBeEquivalentTo(expectedFileParsedEvent,
                options => options
                    .Excluding(p => p.TimeStamp)
                    .Excluding(p => p.Version)
                );
        }

        [Theory, AutoData]
        public async Task Send_command_with_mask_3_should_to_publish_one_FileParsed_4ImageExported_events([Frozen]FileParsed expectedFileParsedEvent)
        {
            var blobId = await fixture.BlobStorage.AddFileAsync("bph12781.pdf", Resource.bph12781, "application/pdf", BUCKET);

            fixture.ClearAllEvents();

            await fixture.Bus.Send(new ParseFile(expectedFileParsedEvent.Id, expectedFileParsedEvent.CorrelationId, expectedFileParsedEvent.UserId, BUCKET, blobId, 3));

            fixture.AllEvents.Count().ShouldBeEquivalentTo(6);
            fixture.AllEvents.Count(e => e is FileParseFailed).ShouldBeEquivalentTo(0);
            fixture.AllEvents.Count(e => e is ImageExported).ShouldBeEquivalentTo(4);
            fixture.AllEvents.Count(e => e is TextExported).ShouldBeEquivalentTo(1);
            fixture.AllEvents.Count(e => e is FileParsed).ShouldBeEquivalentTo(1);

            var @event = fixture.AllEvents.Last();
            expectedFileParsedEvent.ByteTypes = 3;

            @event.ShouldBeEquivalentTo(expectedFileParsedEvent,
                options => options
                    .Excluding(p => p.TimeStamp)
                    .Excluding(p => p.Version)
                );
        }

        [Theory, AutoData]
        public async Task Send_command_with_mask_2_should_to_publish_4ImageExported_events([Frozen]FileParsed expectedFileParsedEvent)
        {
            var blobId = await fixture.BlobStorage.AddFileAsync("bph12781.pdf", Resource.bph12781, "application/pdf", BUCKET);

            fixture.ClearAllEvents();

            await fixture.Bus.Send(new ParseFile(expectedFileParsedEvent.Id, expectedFileParsedEvent.CorrelationId, expectedFileParsedEvent.UserId, BUCKET, blobId, 2));

            fixture.AllEvents.Count().ShouldBeEquivalentTo(5);
            fixture.AllEvents.Count(e => e is FileParseFailed).ShouldBeEquivalentTo(0);
            fixture.AllEvents.Count(e => e is ImageExported).ShouldBeEquivalentTo(4);
            fixture.AllEvents.Count(e => e is TextExported).ShouldBeEquivalentTo(0);
            fixture.AllEvents.Count(e => e is FileParsed).ShouldBeEquivalentTo(1);

            var @event = fixture.AllEvents.Last();
            expectedFileParsedEvent.ByteTypes = 2;

            @event.ShouldBeEquivalentTo(expectedFileParsedEvent,
                options => options
                    .Excluding(p => p.TimeStamp)
                    .Excluding(p => p.Version)
                );
        }

        [Theory, AutoData]
        public async Task Send_command_with_mask_1_should_to_publish_one_FileParsed_one_TextExported_events([Frozen]FileParsed expectedFileParsedEvent)
        {
            var blobId = await fixture.BlobStorage.AddFileAsync("bph12781.pdf", Resource.bph12781, "application/pdf", BUCKET);

            fixture.ClearAllEvents();

            await fixture.Bus.Send(new ParseFile(expectedFileParsedEvent.Id, expectedFileParsedEvent.CorrelationId, expectedFileParsedEvent.UserId, BUCKET, blobId, 1));

            fixture.AllEvents.Count().ShouldBeEquivalentTo(2);
            fixture.AllEvents.Count(e => e is FileParseFailed).ShouldBeEquivalentTo(0);
            fixture.AllEvents.Count(e => e is ImageExported).ShouldBeEquivalentTo(0);
            fixture.AllEvents.Count(e => e is TextExported).ShouldBeEquivalentTo(1);
            fixture.AllEvents.Count(e => e is FileParsed).ShouldBeEquivalentTo(1);

            var @event = fixture.AllEvents.Last();
            expectedFileParsedEvent.ByteTypes = 1;

            @event.ShouldBeEquivalentTo(expectedFileParsedEvent,
                options => options
                    .Excluding(p => p.TimeStamp)
                    .Excluding(p => p.Version)
                );
        }

        [Theory, AutoData]
        public async Task Send_command_to_extract_meta_should_to_publish_one_MetaExtracted_event([Frozen]MetaExtracted expectedEvent)
        {
            var blobId = await fixture.BlobStorage.AddFileAsync("ADMET_IN_SILICOMODELLING___TOWARDS_PREDICTION_PARADISE.pdf", Resource.ADMET_IN_SILICOMODELLING___TOWARDS_PREDICTION_PARADISE, "application/pdf", BUCKET);

            fixture.ClearAllEvents();

            await fixture.Bus.Send(new ExtractMeta(expectedEvent.Id, expectedEvent.CorrelationId, expectedEvent.UserId, BUCKET, blobId));

            fixture.AllEvents.Count().ShouldBeEquivalentTo(1);
            fixture.AllEvents.Count(e => e is MetaExtracted).ShouldBeEquivalentTo(1);

            var @event = fixture.AllEvents.Last() as MetaExtracted;
            expectedEvent.Meta = @event.Meta;
            expectedEvent.BlobId = blobId;
            expectedEvent.Bucket = BUCKET;

            @event.ShouldBeEquivalentTo(expectedEvent,
                options => options
                    .Excluding(p => p.TimeStamp)
                    .Excluding(p => p.Version)
                );
        }

    }
}
