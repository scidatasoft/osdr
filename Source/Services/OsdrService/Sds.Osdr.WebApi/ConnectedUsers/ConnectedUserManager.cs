using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.ConnectedUsers
{
    public class ConnectedUserManager : IConnectedUserManager
    {
        IDictionary<Guid, Guid> _users = new Dictionary<Guid, Guid>();
        private object _sync = new object();

        public void SetCurrentNode(Guid userId, Guid nodeId)
        {
            lock (_sync)
            {
                _users[userId] = nodeId;
            }
        }

        public Guid? GetCurrentNode(Guid userId)
        {
            return _users.ContainsKey(userId) ? (Guid?)_users[userId] : null;
        }

        public void Remove(Guid userId)
        {
            lock (_sync)
            {
                _users.Remove(userId);
            }
        }
    }
}
