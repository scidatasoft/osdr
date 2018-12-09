using FluentAssertions;
using MassTransit;
using AutoFixture.Xunit2;
using Sds.ChemicalStandardizationValidation.Domain.Commands;
using Sds.ChemicalStandardizationValidation.Domain.Events;
using Sds.ChemicalStandardizationValidation.Domain.Models;
using Sds.MassTransit.Extensions;
using Sds.Storage.Blob.Core;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sds.ChemicalStandardizationValidation.Tests
{
    public class ValidatedEvent : Validated
    {
        public ValidatedRecord Record { get; set; }
        public Guid Id { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid UserId { get; set; }
        public DateTimeOffset TimeStamp { get; }
    }

    public partial class StandardizationTests : IClassFixture<StandardizationValidationTestFixture>
    {
        private const string BUCKET = "UnitTests";

        private readonly StandardizationValidationTestFixture _fixture;

        public StandardizationTests(StandardizationValidationTestFixture fixture)
        {
            this._fixture = fixture;
        }

        [Theory, AutoData]
        public async Task Send_command_to_standardize_valid_mol_should_publish_one_Standardized_event([Frozen]ValidatedEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("1oir_canon.mol", Resource._1oir_canon, "chemical/x-mdl-molfile", BUCKET);

                await _fixture.Harness.Bus.Publish<Standardize>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<Standardized>();

                var allEvents = _fixture.Harness.Published.ToList();

                var standardized = allEvents.Select<Standardized>().FirstOrDefault();
                standardized.Should().NotBeNull();
                standardized.ShouldBeEquivalentTo(expectedEvent,
                    options => options
                        .Excluding(p => p.TimeStamp)
                        .Excluding(p => p.Record)
                );
                standardized.Record.Issues.Count().Should().Be(0);
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_standardize_invalid_mol_should_publish_one_StandardizationFailed_event([Frozen]ValidatedEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("1oir_canon_trash_modified.mol", Resource._1oir_canon_trash_modified, "chemical/x-mdl-molfile", BUCKET);

                await _fixture.Harness.Bus.Publish<Standardize>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<StandardizationFailed>();

                var allEvents = _fixture.Harness.Published.ToList();

                var failed = allEvents.Select<StandardizationFailed>().FirstOrDefault();
                failed.Should().NotBeNull();
                failed.ShouldBeEquivalentTo(expectedEvent,
                    options => options
                        .Excluding(p => p.TimeStamp)
                        .Excluding(p => p.Message)
                );
                failed.Message.Should().StartWith($"Blob with id {blobId} from bucket {BUCKET} can not be standardized or not found. Error:");
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_standardize_trash_mol_should_publish_one_StandardizationFailed_event([Frozen]ValidatedEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("1oir_canon_trash_modified.mol", Resource._1oir_canon_trash_modified, "chemical/x-mdl-molfile", BUCKET);

                await _fixture.Harness.Bus.Publish<Standardize>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<StandardizationFailed>();

                var allEvents = _fixture.Harness.Published.ToList();

                var failed = allEvents.Select<StandardizationFailed>().FirstOrDefault();
                failed.Should().NotBeNull();
                failed.ShouldBeEquivalentTo(expectedEvent,
                    options => options
                        .Excluding(p => p.TimeStamp)
                        .Excluding(p => p.Message)
                );
                failed.Message.Should().StartWith($"Blob with id {blobId} from bucket {BUCKET} can not be standardized or not found. Error:");
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_standardize_empty_mol_should_publish_one_StandardizationFailed_event([Frozen]ValidatedEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("empty.mol", Resource.empty, "chemical/x-mdl-molfile", BUCKET);

                await _fixture.Harness.Bus.Publish<Standardize>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<StandardizationFailed>();

                var allEvents = _fixture.Harness.Published.ToList();

                var failed = allEvents.Select<StandardizationFailed>().FirstOrDefault();
                failed.Should().NotBeNull();
                failed.ShouldBeEquivalentTo(expectedEvent,
                    options => options
                        .Excluding(p => p.TimeStamp)
                        .Excluding(p => p.Message)
                );
                failed.Message.Should().StartWith($"Blob with id {blobId} from bucket {BUCKET} can not be standardized or not found. Error:");
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_standardize_nonexistent_mol_should_publish_one_StandardizationFailed_event([Frozen]ValidatedEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = NewId.NextGuid();

                await _fixture.Harness.Bus.Publish<Standardize>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<StandardizationFailed>();

                var allEvents = _fixture.Harness.Published.ToList();

                var failed = allEvents.Select<StandardizationFailed>().FirstOrDefault();
                failed.Should().NotBeNull();
                failed.ShouldBeEquivalentTo(expectedEvent,
                    options => options
                        .Excluding(p => p.TimeStamp)
                        .Excluding(p => p.Message)
                );
                failed.Message.Should().StartWith($"Blob with id {blobId} from bucket {BUCKET} can not be standardized or not found. Error:");
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }
    }
}
