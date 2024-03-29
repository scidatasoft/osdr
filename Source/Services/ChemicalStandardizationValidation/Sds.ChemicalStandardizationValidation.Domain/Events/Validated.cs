﻿using Sds.ChemicalStandardizationValidation.Domain.Models;
using MassTransit;
using System;

namespace Sds.ChemicalStandardizationValidation.Domain.Events
{
    public interface Validated : CorrelatedBy<Guid>
    {
        ValidatedRecord Record { get; }
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
