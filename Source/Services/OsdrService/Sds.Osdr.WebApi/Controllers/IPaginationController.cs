using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Sds.Osdr.WebApi.Extensions;
using Sds.Osdr.WebApi.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Controllers
{
    public interface IPaginationController
    {
        HttpResponse Response { get; }
        string CreatePageUri(PaginationRequest request, PaginationUriType uriType, string action, Guid? containerId = null, string filter = null, IEnumerable<string> fields = null);
        string CreatePageUri(PaginationRequest request, PaginationUriType uriType, string action, RouteValueDictionary routeValueDictionary);
    }
}
