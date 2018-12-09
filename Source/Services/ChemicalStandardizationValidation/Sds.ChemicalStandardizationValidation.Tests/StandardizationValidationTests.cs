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
    public class StandardizationValidationFailedEvent : StandardizationValidationFailed
    {
        public string Message { get; set; }
        public Guid Id { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid UserId { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
    }

    public partial class StandardizationTests : IClassFixture<StandardizationValidationTestFixture>
    {
        [Theory, AutoData]
        public async Task Send_command_to_validate_valid_mol_should_publish_one_ValidatedStandardized_event([Frozen]ValidatedEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("1oir_canon.mol", Resource._1oir_canon, "chemical/x-mdl-molfile", BUCKET);

                await _fixture.Harness.Bus.Publish<ValidateStandardize>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<ValidatedStandardized>();

                var allEvents = _fixture.Harness.Published.ToList();

                var standardized = allEvents.Select<ValidatedStandardized>().FirstOrDefault();
                standardized.Should().NotBeNull();
                standardized.ShouldBeEquivalentTo(expectedEvent,
                    options => options
                        .Excluding(p => p.TimeStamp)
                        .Excluding(p => p.Record)
                );
                standardized.Record.Issues.Count().Should().Be(1);
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_validate_invalid_mol_should_publish_one_StandardizationValidationFailed_event([Frozen]ValidatedEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("1oir_canon_trash_modified.mol", Resource._1oir_canon_trash_modified, "chemical/x-mdl-molfile", BUCKET);

                await _fixture.Harness.Bus.Publish<ValidateStandardize>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<StandardizationValidationFailed>();

                var allEvents = _fixture.Harness.Published.ToList();

                var failed = allEvents.Select<StandardizationValidationFailed>().FirstOrDefault();
                failed.Should().NotBeNull();
                failed.ShouldBeEquivalentTo(expectedEvent,
                    options => options
                        .Excluding(p => p.TimeStamp)
                        .Excluding(p => p.Message)
                );
                failed.Message.Should().StartWith($"Blob with id {blobId} from bucket {BUCKET} can not be validated and standardized or not found. Error:");
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_validate_trash_mol_should_publish_one_StandardizationValidationFailed_event([Frozen]ValidatedEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("1oir_canon_trash_modified.mol", Resource._1oir_canon_trash_modified, "chemical/x-mdl-molfile", BUCKET);

                await _fixture.Harness.Bus.Publish<ValidateStandardize>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<StandardizationValidationFailed>();

                var allEvents = _fixture.Harness.Published.ToList();

                var failed = allEvents.Select<StandardizationValidationFailed>().FirstOrDefault();
                failed.Should().NotBeNull();
                failed.ShouldBeEquivalentTo(expectedEvent,
                    options => options
                        .Excluding(p => p.TimeStamp)
                        .Excluding(p => p.Message)
                );
                failed.Message.Should().StartWith($"Blob with id {blobId} from bucket {BUCKET} can not be validated and standardized or not found. Error:");
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_validate_empty_mol_should_publish_one_StandardizationValidationFailed_event([Frozen]StandardizationValidationFailedEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("empty.mol", Resource.empty, "chemical/x-mdl-molfile", BUCKET);

                await _fixture.Harness.Bus.Publish<ValidateStandardize>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<StandardizationValidationFailed>();

                var allEvents = _fixture.Harness.Published.ToList();

                var failed = allEvents.Select<StandardizationValidationFailed>().FirstOrDefault();
                failed.Should().NotBeNull();
                failed.ShouldBeEquivalentTo(expectedEvent,
                    options => options
                        .Excluding(p => p.TimeStamp)
                        .Excluding(p => p.Message)
                );
                failed.Message.Should().StartWith($"Blob with id {blobId} from bucket {BUCKET} can not be validated and standardized or not found. Error:");
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_validate_standardize_nonexistent_mol_should_publish_one_failed_event([Frozen]ValidatedEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = NewId.NextGuid();

                await _fixture.Harness.Bus.Publish<ValidateStandardize>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<StandardizationValidationFailed>();

                var allEvents = _fixture.Harness.Published.ToList();

                var failed = allEvents.Select<StandardizationValidationFailed>().FirstOrDefault();
                failed.Should().NotBeNull();
                failed.ShouldBeEquivalentTo(expectedEvent,
                    options => options
                        .Excluding(p => p.TimeStamp)
                        .Excluding(p => p.Message)
                );
                failed.Message.Should().StartWith($"Blob with id {blobId} from bucket {BUCKET} can not be validated and standardized or not found. Error:");
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }
    }
}