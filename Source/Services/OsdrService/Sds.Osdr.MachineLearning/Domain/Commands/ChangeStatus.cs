using System;
using System.Collections.Generic;
using System.Text;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public interface ChangeStatus
    {
        Guid Id { get; set; }
        ModelStatus Status { get; }
        Guid UserId { get; set; }
    }
}
