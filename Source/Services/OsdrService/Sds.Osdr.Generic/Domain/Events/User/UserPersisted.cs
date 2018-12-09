using CQRSlite.Events;

namespace Sds.Osdr.Generic.Domain.Events.Users
{
    public interface UserPersisted : IEvent
    {
        string DisplayName { get; }
        string FirstName { get; }
        string LastName { get; }
    }
}
