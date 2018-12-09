using Sds.Osdr.Generic.Domain.ValueObjects;
using System;

namespace Sds.Osdr.Generic.Domain.Commands.Files
{
    public interface AddImage
    {
        Guid Id { get; set; }
        Image Image { get; }
        Guid UserId { get; set; }
    }
}
