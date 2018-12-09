using Elasticsearch.Net;
using MassTransit;
using Nest;
using Sds.Osdr.Generic.Domain.Events.Users;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Sds.Indexing.EventHandlers
{
    public class UserEventHandlers : IConsumer<UserPersisted>
    {
        IElasticClient _elasticClient;

        public UserEventHandlers(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient ?? throw new ArgumentNullException(nameof(elasticClient));
        }

        public async Task Consume(ConsumeContext<UserPersisted> context)
        {
            var alias = await _elasticClient.AliasExistsAsync(new AliasExistsDescriptor().Name(context.Message.Id.ToString()));
            if (alias.Exists)
            {
                await _elasticClient.AliasAsync(a => a
                    .Remove(r => r.Index("records").Alias(context.Message.Id.ToString()))
                    .Remove(r => r.Index("folders").Alias(context.Message.Id.ToString()))
                    .Remove(r => r.Index("files").Alias(context.Message.Id.ToString()))
                    .Remove(r => r.Index("models").Alias(context.Message.Id.ToString())));
            }

            try
            {
                await _elasticClient.AliasAsync(a => a
                    .Add(add => add.Index("files").Alias(context.Message.Id.ToString())
                        .Filter<dynamic>(qc => qc.Bool(b => b.Should(s => s.Term("OwnedBy", context.Message.Id.ToString()), s => s.Term("IsPublic", true)))))
                    .Add(add => add.Index("folders").Alias(context.Message.Id.ToString())
                        .Filter<dynamic>(qc => qc.Bool(b => b.Should(s => s.Term("OwnedBy", context.Message.Id.ToString()), s => s.Term("IsPublic", true)))))
                    .Add(add => add.Index("records").Alias(context.Message.Id.ToString())
                        .Filter<dynamic>(qc => qc.Bool(b => b.Should(s => s.Term("OwnedBy", context.Message.Id.ToString()), s => s.Term("IsPublic", true)))))
                    .Add(add => add.Index("models").Alias(context.Message.Id.ToString())
                        .Filter<dynamic>(qc => qc.Bool(b => b.Should(s => s.Term("OwnedBy", context.Message.Id.ToString()), s => s.Term("IsPublic", true)))))
                  );

                Log.Information($"Alias {context.Message.Id} successfully created");
            }
            catch (ElasticsearchClientException e)
            {
                Log.Error($"Creating alias server error: {e.Response.ServerError}");
            }
        }
    }
}
