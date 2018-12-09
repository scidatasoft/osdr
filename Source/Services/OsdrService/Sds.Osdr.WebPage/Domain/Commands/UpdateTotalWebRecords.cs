using System;

namespace Sds.Osdr.WebPage.Domain.Commands
{
    public interface UpdateTotalWebRecords
    {
        long TotalRecords { get; set; }
        Guid Id { get; set; }
        Guid UserId { get; set; }
        int ExpectedVersion { get; set; }
    }
}
