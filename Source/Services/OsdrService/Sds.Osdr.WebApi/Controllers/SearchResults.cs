using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nest;
using Sds.Osdr.WebApi.Extensions;
using Sds.Osdr.WebApi.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using Sds.Osdr.WebApi.Responses;
using Serilog;
using Sds.Osdr.WebApi.Filters;
using Microsoft.AspNetCore.Routing;
using System.Dynamic;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Sds.Osdr.WebApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [UserInfoRequired]
    public class SearchResults:Controller, IPaginationController
    {
        IElasticClient _elasticClient;
        private IUrlHelper _urlHelper;
        public SearchResults(IElasticClient elasticClient, IUrlHelper urlHelper)
        {
            _urlHelper = urlHelper ?? throw new ArgumentNullException(nameof(urlHelper));
            _elasticClient = elasticClient??throw new ArgumentNullException(nameof(elasticClient));
        }

        [HttpGet("", Name = "GetSearchResults")]
        public IActionResult Get(string query, PaginationRequest paginationRequest)
        {
            new[] { "(", ")" }.ToList().ForEach(s => query = query.Replace(s, $"\\{s}"));

            if (!Regex.Match(query, @"^"".*""$").Success)
                query = $"*{query}*";

            if (string.IsNullOrEmpty(query))
                query = "*";

            Log.Information($"Searching in elasticsearch: {query}, user: '{User.Claims.Where(c => c.Type.Contains("/nameidentifier")).First().Value}'");

            var result = _elasticClient.Search<dynamic>(s => s
                .AllTypes()
                .Index(User.Claims.Where(c => c.Type.Contains("/nameidentifier")).First().Value)
                .Source(src => src.Excludes(ex => ex.Field("Blob.ParsedContent")))
                .From((paginationRequest.PageNumber - 1) * paginationRequest.PageSize)
                .Take(paginationRequest.PageSize)
                    .Query(q =>
                        q.QueryString(qs =>
                            qs.Query(query))));

            var list = new PagedList<dynamic>(result.Hits.Select(h => JsonConvert.DeserializeObject<ExpandoObject>(h.Source.Node.ToString())), (int)result.Total, paginationRequest.PageNumber, paginationRequest.PageSize);

            this.AddPaginationHeader(paginationRequest, list, "GetSearchResults", null, query);

            return Ok(list);
        }

        [NonAction]
        public string CreatePageUri(PaginationRequest request, PaginationUriType uriType, string action, Guid? containerId = default(Guid?), string filter = null, IEnumerable<string> fields = null)
        {
            int pageNumber = uriType == PaginationUriType.PreviousPage ? request.PageNumber - 1 : request.PageNumber + 1;

            return _urlHelper.Link(action, new { query= filter, pageSize = request.PageSize, pageNumber });
        }

        [NonAction]
        public string CreatePageUri(PaginationRequest request, PaginationUriType uriType, string action, RouteValueDictionary routeValueDictionary)
        {
            int pageNumber = uriType == PaginationUriType.PreviousPage ? request.PageNumber - 1 : request.PageNumber + 1;

            return _urlHelper.Link(action, routeValueDictionary);
        }
    }
}
