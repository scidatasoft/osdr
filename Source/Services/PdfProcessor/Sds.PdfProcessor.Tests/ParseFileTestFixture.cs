using CQRSlite.Bus;
using CQRSlite.Events;
using Sds.CqrsLite.Moq;
using Sds.PdfProcessor.Domain.Commands;
using Sds.PdfProcessor.Processing.CommandHandlers;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.InMemory;
using System;
using System.Collections.Generic;

namespace Sds.ChemicalFileParser.Tests
{
    public class ParseFileTestFixture : IDisposable
    {
        public Guid UserId { get; private set; }

        public InProcessBus Bus { get; } = new InProcessBus();

        public IBlobStorage BlobStorage { get; } = new InMemoryStorage();

        public IList<IEvent> AllEvents { get; } = new List<IEvent>();

        public ParseFileTestFixture()
        {
            UserId = Guid.NewGuid();

            var eventPublisherMock = new MockEventPublisher((e) => {
                AllEvents.Add(e);
            });

            var handler = new ParseFileCommandHandler(BlobStorage, eventPublisherMock.Object);
            Bus.RegisterHandler<ParseFile>(handler.Handle);

            var extractHandler = new ExtractMetaCommandHandler(BlobStorage, eventPublisherMock.Object);
            Bus.RegisterHandler<ExtractMeta>(extractHandler.Handle);
        }

        public void Dispose()
        {
        }

        public void ClearAllEvents()
        {
            AllEvents.Clear();
        }
    }
}
