using CQRSlite.Events;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CQRSlite.Domain.Exception;
using System.Threading;

namespace Sds.CqrsLite.Events
{
    public class InMemoryEventStore : IEventStore
    {
        private IDictionary<Guid, IList<IEvent>> _streams = new Dictionary<Guid, IList<IEvent>>();

        public Task<IEnumerable<IEvent>> Get(Guid aggregateId, int fromVersion, CancellationToken token = default(CancellationToken))
        {
            lock (_streams)
            {
                if (_streams.ContainsKey(aggregateId))
                {
                    var events = _streams[aggregateId].Skip(fromVersion);

                    return Task.FromResult<IEnumerable<IEvent>>(events);
                }

                return Task.FromResult<IEnumerable<IEvent>>(new List<IEvent>());
            }
        }

        public Task Save(IEnumerable<IEvent> events, CancellationToken token = default(CancellationToken))
        {
            lock (_streams)
            {
                var groupedEvents = events.GroupBy(e => e.Id, (id, evnts) => new
                {
                    Id = id,
                    Events = evnts
                });

                foreach (var g in groupedEvents)
                {
                    AppendEventsToStream(g.Id, g.Events, g.Events.First().Version - 1);
                }

                return Task.CompletedTask;
            }
        }

        private Task AppendEventsToStream(Guid id, IEnumerable<IEvent> domainEvents, int expectedVersion)
        {
            lock (_streams)
            {
                if (!_streams.ContainsKey(id))
                {
                    _streams[id] = new List<IEvent>();
                }

                var stream = _streams[id];

                var lastEvent = stream.LastOrDefault();

                if (lastEvent == null && expectedVersion != 0 || lastEvent != null && lastEvent.Version != expectedVersion)
                {
                    throw new ConcurrencyException(id);
                }

                foreach (var e in domainEvents)
                {
                    stream.Add(e);
                }

                return Task.CompletedTask;
            }
        }
    }
}
