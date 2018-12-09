using CQRSlite.Events;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CQRSlite.Domain.Exception;
using System.Threading;
using Sds.CqrsLite.Saga;

namespace Sds.CqrsLite.Events
{
    public class InMemoryStateRepository : IStateRepository
    {
        private IDictionary<Guid, IState> _states = new Dictionary<Guid, IState>();

        public Task<TState> Get<TState>(Guid id) where TState : IState
        {
            lock (_states)
            {
                if (_states.ContainsKey(id) && _states[id].GetType().Equals(typeof(TState)))
                {
                    return Task.FromResult<TState>((TState)_states[id]);
                }

                return Task.FromResult<TState>(default(TState));
            }
        }

        public Task Remove(Guid id)
        {
            lock (_states)
            {
                if (_states.ContainsKey(id))
                {
                    _states.Remove(id);
                }

                return Task.CompletedTask;
            }
        }

        public Task Save<TState>(TState state, long? expectedVersion = default(long?)) where TState : IState
        {
            lock (_states)
            {
                if (_states.ContainsKey(state.Id) && expectedVersion != null && expectedVersion != 0)
                {
                    if (_states[state.Id].Version != expectedVersion)
                    {
                        throw new ConcurrencyException(state.Id);
                    }
                }

                _states[state.Id] = state;

                return Task.CompletedTask;
            }
        }
    }
}
