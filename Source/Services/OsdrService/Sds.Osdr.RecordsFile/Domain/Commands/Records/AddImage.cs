using Sds.Osdr.Generic.Domain.ValueObjects;
using System;

namespace Sds.Osdr.RecordsFile.Domain.Commands.Records
{
    public interface AddImage
    {
        Image Image { get; }
        Guid Id { get; }
        Guid UserId { get; }
    }
}
