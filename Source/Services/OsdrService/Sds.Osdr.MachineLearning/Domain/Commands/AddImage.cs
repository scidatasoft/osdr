using Sds.Osdr.Generic.Domain.ValueObjects;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public interface AddImage
    {
        Guid Id { get; set; }
        Image Image { get; }
        Guid UserId { get; set; }
    }
}
