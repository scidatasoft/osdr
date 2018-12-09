using System;

namespace Sds.Osdr.RecordsFile.Domain.Commands.Records
{
    public interface DeleteRecord
    {
        Guid Id { get; }
        Guid UserId { get; }
        int ExpectedVersion { get; }
        bool Force { get; }
	}
}
