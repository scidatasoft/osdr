namespace Sds.Osdr.Domain.AccessControl
{
    public interface IAccessControl
    {
        bool IsServiceAvailable<T>() where T : class;
    }
}
