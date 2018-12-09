using Sds.Storage.Blob.Events;
using System.Threading.Tasks;

namespace Sds.Osdr.Domain.Modules
{
    public interface IModule
    {
        bool IsSupported(BlobLoaded blob);
        Task Process(BlobLoaded blob);
    }
}
