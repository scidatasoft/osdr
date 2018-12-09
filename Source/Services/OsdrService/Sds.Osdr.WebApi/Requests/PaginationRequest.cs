using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Requests
{
    public class PaginationRequest
    {
        const int maxPageSize = 500;
        public int PageNumber { get; set; } = 1;

        private int _pageSize = maxPageSize / 2;
        public int PageSize
        {
            get { return _pageSize; }
            set { _pageSize = (value > maxPageSize) ? maxPageSize : value; }
        }
    }
}
