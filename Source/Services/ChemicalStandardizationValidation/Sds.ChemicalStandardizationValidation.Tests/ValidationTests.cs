using AutoFixture.Xunit2;
using FluentAssertions;
using MassTransit;
using Sds.ChemicalStandardizationValidation.Domain.Commands;
using Sds.ChemicalStandardizationValidation.Domain.Events;
using Sds.MassTransit.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sds.ChemicalStandardizationValidation.Tests
{
    public partial class StandardizationTests : IClassFixture<StandardizationValidationTestFixture>
    {
        [Theory, AutoData]
        public async Task Send_command_to_validate_valid_mol_should_publish_one_Validated_event([Frozen]ValidatedEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("1oir_canon.mol", Resource._1oir_canon, "chemical/x-mdl-molfile", BUCKET);

                await _fixture.Harness.Bus.Publish<Validate>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<Validated>();

                var allEvents = _fixture.Harness.Published.ToList();

                var validated = allEvents.Select<Validated>().FirstOrDefault();
                validated.Should().NotBeNull();
                validated.ShouldBeEquivalentTo(expectedEvent,
                    options => options
                        .Excluding(p => p.TimeStamp)
                        .Excluding(p => p.Record)
                );
                validated.Record.Issues.Count().Should().Be(1);
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_validate_invalid_mol_should_publish_one_Validated_event([Frozen]ValidatedEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("1oir_canon_trash_modified.mol", Resource._1oir_canon_trash_modified, "chemical/x-mdl-molfile", BUCKET);

                await _fixture.Harness.Bus.Publish<Validate>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<Validated>();

                var allEvents = _fixture.Harness.Published.ToList();

                var validated = allEvents.Select<Validated>().FirstOrDefault();
                validated.Should().NotBeNull();
                validated.ShouldBeEquivalentTo(expectedEvent,
                    options => options
                        .Excluding(p => p.TimeStamp)
                        .Excluding(p => p.Record)
                );
                validated.Record.Issues.Count().Should().Be(2);
                validated.Record.Issues.Where(i => i.Severity == Domain.Models.Severity.Error).Count().Should().Be(1);
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_validate_trash_mol_should_publish_one_Validated_event([Frozen]ValidatedEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("1oir_canon_trash_modified.mol", Resource._1oir_canon_trash_modified, "chemical/x-mdl-molfile", BUCKET);

                await _fixture.Harness.Bus.Publish<Validate>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<Validated>();

                var allEvents = _fixture.Harness.Published.ToList();

                var validated = allEvents.Select<Validated>().FirstOrDefault();
                validated.Should().NotBeNull();
                validated.ShouldBeEquivalentTo(expectedEvent,
                    options => options
                        .Excluding(p => p.TimeStamp)
                        .Excluding(p => p.Record)
                );
                validated.Record.Issues.Count().Should().Be(2);
                validated.Record.Issues.Where(i => i.Severity == Domain.Models.Severity.Error).Count().Should().Be(1);
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_validate_empty_mol_should_publish_one_Validated_event([Frozen]ValidatedEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("empty.mol", Resource.empty, "chemical/x-mdl-molfile", BUCKET);

                await _fixture.Harness.Bus.Publish<Validate>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<Validated>();

                var allEvents = _fixture.Harness.Published.ToList();

                var validated = allEvents.Select<Validated>().FirstOrDefault();
                validated.Should().NotBeNull();
                validated.ShouldBeEquivalentTo(expectedEvent,
                    options => options
                        .Excluding(p => p.TimeStamp)
                        .Excluding(p => p.Record)
                );
                validated.Record.Issues.Count().Should().Be(3);
                validated.Record.Issues.Where(i => i.Severity == Domain.Models.Severity.Error).Count().Should().Be(3);
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_validate_nonexistent_mol_should_publish_one_ValidationFailed_event([Frozen]ValidatedEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = NewId.NextGuid();

                await _fixture.Harness.Bus.Publish<Validate>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<ValidationFailed>();

                var allEvents = _fixture.Harness.Published.ToList();

                var validated = allEvents.Select<ValidationFailed>().FirstOrDefault();
                validated.Should().NotBeNull();
                validated.ShouldBeEquivalentTo(expectedEvent,
                    options => options
                        .Excluding(p => p.TimeStamp)
                        .Excluding(p => p.Message)
                );
                validated.Message.Should().StartWith($"Blob with id {blobId} from bucket {BUCKET} cannot be validated or not found. Error:");
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }
    }
}
