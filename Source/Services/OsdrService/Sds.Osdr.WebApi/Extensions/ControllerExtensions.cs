using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Sds.Osdr.WebApi.Controllers;
using Sds.Osdr.WebApi.Requests;
using Sds.Osdr.WebApi.Responses;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.WebApi.Extensions
{
    public static class ControllerExtensions
    {
        public static void AddPaginationHeader<T>(this IPaginationController controller, PaginationRequest request, PagedList<T> list, string action, Guid? entityId = null, string filter = null, IEnumerable<string> fields = null)
        {
            string previousPageLink = list.HasPreviousPage ? controller.CreatePageUri(request, PaginationUriType.PreviousPage, action, entityId, filter, fields) : null;
            string nextPageLink = list.HasNextPage ? controller.CreatePageUri(request, PaginationUriType.NextPage, action, entityId, filter, fields) : null;

            var paginationMetadata = new
            {
                totalCount = list.TotalCount,
                pageSize = list.PageSize,
                totalPages = list.TotalPages,
                currentPage = list.CurrentPage,
                nextPageLink,
                previousPageLink
            };
            controller.Response.Headers.Add("Access-Control-Expose-Headers", "X-Pagination");
            controller.Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationMetadata));
        }

        public static void AddPaginationHeader<T>(this IPaginationController controller, PaginationRequest request, DataProviders.PagedList<T> list, string action, RouteValueDictionary routeValueDictionary)
        {
            string previousPageLink = list.HasPreviousPage ? controller.CreatePageUri(request, PaginationUriType.PreviousPage, action, routeValueDictionary) : null;
            string nextPageLink = list.HasNextPage ? controller.CreatePageUri(request, PaginationUriType.NextPage, action, routeValueDictionary) : null;

            var paginationMetadata = new
            {
                totalCount = list.TotalCount,
                pageSize = list.PageSize,
                totalPages = list.TotalPages,
                currentPage = list.CurrentPage,
                nextPageLink,
                previousPageLink
            };
            controller.Response.Headers.Add("Access-Control-Expose-Headers", "X-Pagination");
            controller.Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationMetadata));
        }
    }
}
