using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.ConnectedUsers
{
    public interface IConnectedUserManager
    {
        void SetCurrentNode(Guid userId, Guid nodeId);
        Guid? GetCurrentNode(Guid userId);
        void Remove(Guid userId);

    }
}
