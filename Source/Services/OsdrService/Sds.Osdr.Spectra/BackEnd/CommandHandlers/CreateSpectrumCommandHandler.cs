using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.Spectra.Domain;
using Sds.Osdr.Spectra.Domain.Commands;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Spectra.BackEnd.CommandHandlers
{
    public class CreateSpectrumCommandHandler : IConsumer<CreateSpectrum>
    {
        private readonly ISession session;

        public CreateSpectrumCommandHandler(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<CreateSpectrum> context)
        {
            var spectrum = new Spectrum(context.Message.Id, context.Message.Bucket, context.Message.BlobId, context.Message.UserId, context.Message.FileId, context.Message.Index, context.Message.Fields);

            await session.Add(spectrum);

            await session.Commit();
        }
    }
}
