using MongoDB.Bson;
using MongoDB.Driver;
using Sds.Osdr.WebApi.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Extensions
{
    public static class PagedListExtensions
    {
        public async static Task<PagedList<TProjection>> ToPagedListAsync<TDocument, TProjection>(this IFindFluent<TDocument, TProjection> findFluent, int pageNumber, int pageSize)
        {
            long count = await findFluent.CountAsync();
            var items = await findFluent.Skip(pageSize * (pageNumber - 1))
                    .Limit(pageSize)
                    .ToListAsync();

            return new PagedList<TProjection>(items, (int)count, pageNumber, pageSize);
        }

        public async static Task<PagedList<TProjection>> ToPagedListAsync<TProjection>(this IAggregateFluent<TProjection> findFluent, int pageNumber, int pageSize)
        {
            var count = await findFluent.AnyAsync() ? findFluent.Group(new BsonDocument
            {
                { "_id", "_id" },
                {"count", new BsonDocument("$sum", 1)}
            })
            .FirstAsync().Result["count"].AsInt32 : 0;

            var items = await findFluent.Skip(pageSize * (pageNumber - 1))
                    .Limit(pageSize)
                    .ToListAsync();

            return new PagedList<TProjection>(items, count, pageNumber, pageSize);
        }

        public static PagedList<T> ToPagedList<T>(this IQueryable<T> query, int pageNumber, int pageSize)
        {
            var items = query.Skip(pageSize * (pageNumber - 1)).Take(pageSize).ToList();

            return new PagedList<T>(items, query.Count(), pageNumber, pageSize);
        }
    }
}
