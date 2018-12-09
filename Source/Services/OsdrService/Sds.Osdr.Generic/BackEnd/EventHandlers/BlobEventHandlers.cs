using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.Domain.Modules;
using Sds.Osdr.Generic.Modules;
using Sds.Storage.Blob.Events;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.BackEnd.EventHandlers
{
    public class BlobEventHandlers : IConsumer<BlobLoaded>
    {
        private readonly ISession _session;
        private readonly IEnumerable<IModule> _modules;

        public BlobEventHandlers(ISession session, IEnumerable<IModule> modules)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _modules = modules ?? throw new ArgumentNullException(nameof(modules));
        }

        public async Task Consume(ConsumeContext<BlobLoaded> context)
        {
            if (context.Message.BlobInfo.Metadata.ContainsKey("SkipOsdrProcessing"))
            {
                if (Convert.ToBoolean(context.Message.BlobInfo.Metadata["SkipOsdrProcessing"].ToString()) == true)
                {
                    return;
                }
            }

            var generic = _modules.Single(m => m.GetType() == typeof(GenericModule));
            if (generic == null)
                throw new NullReferenceException(nameof(generic));

            foreach (var module in _modules.Where(m => m != generic))
            {
                if (module.IsSupported(context.Message))
                {
                    await module.Process(context.Message);
                    return;
                }
            }

            await generic.Process(context.Message);
        }
    }
}
