using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Sds.Osdr.Generic.Domain.ValueObjects;
using Sds.Osdr.WebApi.ConnectedUsers;
using Sds.Osdr.WebApi.DataProviders;
using Sds.Osdr.WebApi.Extensions;
using Sds.Osdr.WebApi.Filters;
using Sds.Osdr.WebApi.Models;
using Sds.Osdr.WebApi.Requests;
using Sds.Osdr.WebApi.Responses;
using Sds.Storage.Blob.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class NodesController : MongoDbController, IPaginationController
    {
        private const string breadcrumbsHeaderName = "X-Breadcrumbs";
        private IUrlHelper _urlHelper;
        private IMongoCollection<BaseNode> _nodes;
        private IMongoCollection<BsonDocument> _accessPermissions;
        private readonly IBlobStorage _blobStorage;
        readonly IConnectedUserManager _connectedUserManager;
        readonly IOrganizeDataProvider _dataProvider;

        public NodesController(IMongoDatabase database, IBlobStorage blobStorage, IUrlHelper urlHelper, IConnectedUserManager connectedUserManager, IOrganizeDataProvider dataProvider)
            : base(database)
        {
            _urlHelper = urlHelper ?? throw new ArgumentNullException(nameof(urlHelper));
            _nodes = Database.GetCollection<BaseNode>("Nodes");
            _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
            _connectedUserManager = connectedUserManager ?? throw new ArgumentNullException(nameof(connectedUserManager));
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(IOrganizeDataProvider));
            _accessPermissions = Database.GetCollection<BsonDocument>("AccessPermissions");
        }
        
        [HttpGet("{id}")]
        [HttpHead("{id}")]
        [AllowSharedContent]
        [ResponseCache(Duration = 160)]
        public async Task<IActionResult> Get(Guid id, [FromQuery(Name = "$projection"), ModelBinder(BinderType = typeof(ArrayModelBinder))]IEnumerable<string> fields)
        {
            Log.Information($"Getting node {id}");
                        
            var filter = new OrganizeFilter().ById(id);

            if (fields is null)
                fields = Array.Empty<string>();

            var node = await GetNodeAggregation(filter).Project<dynamic>(fields?.ToMongoDBProjection()).FirstOrDefaultAsync();

            if (node is null)
            {
                Log.Information($"Node {id} not found.");
                return NotFound();
            }

            var breadcrumbs = _dataProvider.GetBreadcrumbs(id).Reverse();

            breadcrumbs.AsParallel()
                .ForAll(p => p.Name = WebUtility.UrlEncode(p.Name)?.Replace("+", "%20"));

            Response.Headers["Access-Control-Expose-Headers"] += breadcrumbsHeaderName;
            Response.Headers.Add(breadcrumbsHeaderName, JsonConvert.SerializeObject(breadcrumbs));

            return Ok(node);
        }
        
        [AllowAnonymous]
        [HttpGet("public", Name = nameof(NodesPublic))]
        [HttpHead("public", Name = nameof(NodesPublic))]
        public async Task<IActionResult> NodesPublic([FromQuery(Name = "$filter")]string filter,
            [FromQuery]PaginationRequest request,
            [FromQuery(Name = "$projection"), ModelBinder(BinderType = typeof(ArrayModelBinder))]IEnumerable<string> fields)
        {
            if (!fields?.Select(s => s.ToLower()).Contains("id") ?? false)
            {
                var fieldList = new List<string>();
                fieldList.AddRange(fields);
                fieldList.Add("Id");
                fields = fieldList;
            }
            
            BsonDocument filterDocument = null;
            try
            {
                filterDocument = new OrganizeFilter().ByQueryString(filter);
            }
            catch
            {
                Log.Error($"Filter syntax error '{filter}'");
                return BadRequest();
            }
            
            var projection = fields.ToMongoDBProjection();

            var aggregation = _accessPermissions.Aggregate()
                .Match(new BsonDocument("IsPublic", true))
                .Lookup<BsonDocument, BsonDocument>("Nodes", "_id", "_id", "Nodes")
                .Unwind("Nodes", new AggregateUnwindOptions<BsonDocument> {PreserveNullAndEmptyArrays = true})
                .GraphLookup(_nodes, "Nodes.ParentId", "_id", "$Nodes.ParentId", "Parents")
                .Match(new BsonDocument("$or", new BsonArray(
                    new[]
                    {
                        new BsonDocument("Parents", new BsonDocument("$size", 0)),
                        new BsonDocument("Parents.IsDeleted", new BsonDocument("$ne", true))
                    })))
                .AppendStage<BsonDocument>(BsonDocument.Parse(@"
                {
                    $addFields: {
                        'Nodes.Order':  { 
                            $cond: { if: { $eq: ['$Type', 'Folder'] }, then: 1, else: 100 } 
                        },
                        'Nodes.AccessPermissions._id': ""$_id"",
                        'Nodes.AccessPermissions.IsPublic': ""$IsPublic"",
                        'Nodes.AccessPermissions.Users': ""$Users"",
                        'Nodes.AccessPermissions.Groups': ""$Groups""
                    }
                }"))
                .ReplaceRoot(new BsonValueAggregateExpressionDefinition<BsonDocument, BsonDocument>(@"$Nodes"))
                .Sort("{ Order:1, Name:1 }")
                .Project("{ Order: 0 }")
                .Match(filterDocument)
                .Project<dynamic>(projection);
                
            
            var list = await aggregation.ToPagedListAsync(request.PageNumber, request.PageSize);

            this.AddPaginationHeader(request, list, nameof(NodesPublic), null, filter, fields);
            
            return Ok(list);
        }
        
        /// <summary>
        /// Get nodes list in selected node
        /// </summary>
        /// <param name="id">node id or 'me' for root user node</param>
        /// <param name="type">type for inner node</param>
        /// <param name="status">node status</param>
        /// <param name="request">pagination request</param>
        /// <returns></returns>
        [HttpGet("{id:guid}/nodes", Name = nameof(GetInnerNodes))]
        [HttpHead("{id:guid}/nodes")]
        [HttpGet("me/nodes", Name = "GetNodesInRoot")]
        [HttpHead("me/nodes")]
        [AllowSharedContent]
        public async Task<IActionResult> GetInnerNodes(Guid? id, string type, [FromQuery]PaginationRequest request, string status = null)
        {
            Log.Information($"Getting nodes from id {id}");
            var list = await GetNodesPagedList(id ?? (UserId ?? Guid.Empty), type, request, status);
            this.AddPaginationHeader(request, list, id.HasValue ? nameof(GetInnerNodes) : "GetNodesInRoot", id);

            return Ok(list);
        }

        [HttpGet("{id:guid}/page", Name = nameof(GetIndexOnPage))]
        public async Task<IActionResult> GetIndexOnPage(Guid id, int pageSize)
        {
            Log.Information($"Getting page from node id {id}");

            var nodes = await _nodes.FindAsync(new BsonDocument("_id", id));
            var node = nodes.Single();
            var filter = new BsonDocument("ParentId", node.ParentId);
            filter.Add("OwnedBy", UserId);

//            var aggregation = _accessPermissions.Aggregate()
            var aggregation = GetNodeAggregation(filter);
//            var list = await aggregation.ToPagedListAsync(1, pageSize);
            var list = await aggregation.ToListAsync();

            var numberPage = 1;
            var indexPage = 0;

            for (var i = 0; i < list.Count; i++)
            {
                var aggregatedNode = list[i];

                if (indexPage == pageSize)
                {
                    numberPage++;
                    indexPage = 0;
                }

                if (aggregatedNode._id == node.Id)
                    break;
                
                indexPage++;
            }
            return Ok(new { pageNumber = numberPage });
        }

        [HttpPost("current")]
        public async Task<IActionResult> SetCurrentNode([FromBody]SetCurrentNodeRequest request)
        {
            if (request is null)
                return BadRequest();

            Log.Information($"Set current node to {request.Id}");
            var filter = (request.Id ?? UserId) ==  UserId ? new OrganizeFilter().ById(UserId.Value) : new OrganizeFilter(UserId.Value).ById(request.Id.Value);
            var node = await GetNodeAggregation(filter).FirstOrDefaultAsync();
            if (node is null)
            {
                Log.Information($"Node {request.Id} not found.");
                return NotFound();
            }

            _connectedUserManager.SetCurrentNode(UserId.Value, request.Id ?? UserId.Value);

            var breadcrumbs = await GetBreadcrumbs(new BsonDocument(node as IDictionary<string, object>));
            Response.Headers["Access-Control-Expose-Headers"] += breadcrumbsHeaderName;
            Response.Headers.Add(breadcrumbsHeaderName, JsonConvert.SerializeObject(breadcrumbs));

            return Ok(node);
        }

//        [AllowAnonymous]
//        [HttpGet(Name = nameof(GetNodeListShared))]
//        [HttpHead]
//        public async Task<IActionResult> GetNodeListShared([FromQuery(Name = "$filter")] string filter,
//            [FromQuery] PaginationRequest request,
//            [FromQuery(Name = "$projection"), ModelBinder(BinderType = typeof(ArrayModelBinder))]
//            IEnumerable<string> fields)
//        {
//            if (!fields?.Select(s => s.ToLower()).Contains("id") ?? false)
//            {
//                var fieldList = new List<string>();
//                fieldList.AddRange(fields);
//                fieldList.Add("Id");
//                fields = fieldList;
//            }

//            BsonDocument filterDocument = null;
//            try
//            {
//                filterDocument = new OrganizeFilter(UserId ?? Guid.Empty).ByQueryString(filter);
//                filterDocument.Remove("OwnedBy");
//                filterDocument.Add(new BsonDocument("$or", new BsonArray(
//                    new[]
//                    {
//                        new BsonDocument("OwnedBy", UserId ?? Guid.Empty),
//                        new BsonDocument("AccessPermissions.IsPublic", true),
//                        new BsonDocument("AccessPermissions.Parents.IsPublic", true)
//                    }
//                )));
//            }
//            catch
//            {
//                Log.Error($"Filter syntax error '{filter}'");
//                return BadRequest();
//            }
            

//            var projection = fields.ToMongoDBProjection();
//            var aggregation = _nodes.Aggregate()
                
//                .GraphLookup(_nodes, "ParentId", "_id", "$ParentId", "Parents")
//                .Match(new BsonDocument("$or", new BsonArray(
//                    new BsonDocument[] {
//                        new BsonDocument("Parents", new BsonDocument("$size", 0)),
//                        new BsonDocument("Parents.IsDeleted", new BsonDocument("$ne", true))
//                    })))
//                .Lookup<BsonDocument, BsonDocument>(nameof(AccessPermissions), "_id", "_id", nameof(AccessPermissions))
//                .Unwind(nameof(AccessPermissions), new AggregateUnwindOptions<BsonDocument> { PreserveNullAndEmptyArrays = true })
//                .Lookup<BsonDocument, BsonDocument>(nameof(AccessPermissions), "Parents._id", "_id", $"{nameof(AccessPermissions)}.Parents")
//                .Match(filterDocument)
//                .Project(@"{ 
//                            Order: { $cond: { if: { $eq: [""$Type"", ""Folder""] }, then: 1, else: 100 } }
//                            Version:1,
//                            Status:1,
//                            Type:1,
//                            CreatedDateTime:1,
//                            UpdatedDateTime:1,
//                            Name:1,
//                            OwnedBy:1,
//                            CreatedBy:1,
//                            Blob:1,
//                            Images:1,
//                            ParentId:1,
//                            UpdatedBy:1,
//                            SubType:1,
//                            TotalRecords:1,
//                            DisplayName:1,
//                            FirstName:1,
//                            LastName:1,
//                            LoginName:1,
//                            Email:1,
//                            Avatar:1,
//                            AccessPermissions:1
//                        }")
//                .Sort("{ Order:1, Name:1 }")
//                .Project<dynamic>("{ Order: 0, AccessPermissions: 0 }")
//                .Project<dynamic>(projection);
            
//            var list = await aggregation.ToPagedListAsync(request.PageNumber, request.PageSize);

//            this.AddPaginationHeader(request, list, nameof(GetNodeList), null, filter, fields);

//            return Ok(list);
//        }

        [Route("me")]
        [HttpGet("me")]
        [Route("")]
        [HttpGet(Name = nameof(GetNodeList))]
        [HttpHead]
        public async Task<IActionResult> GetNodeList([FromQuery(Name = "$filter")]string filter,
            [FromQuery]PaginationRequest request,
            [FromQuery(Name = "$projection"), ModelBinder(BinderType = typeof(ArrayModelBinder))]IEnumerable<string> fields)
        {
            //if (fields is null)
            //    fields = typeof(Node).GetProperties().Select(p => p.Name);

            if (!fields?.Select(s => s.ToLower()).Contains("id") ?? false)
            {
                var fieldList = new List<string>();
                fieldList.AddRange(fields);
                fieldList.Add("Id");
                fields = fieldList;
            }

            BsonDocument filterDocument = null;
            try
            {
                filterDocument = new OrganizeFilter(UserId.Value).ByQueryString(filter);
            }
            catch
            {
                Log.Error($"Filter syntax error '{filter}'");
                return BadRequest();
            }

            var projection = fields.ToMongoDBProjection();
            var aggregation = GetNodeAggregation(filterDocument).Project<dynamic>(projection);

            var list = await aggregation.ToPagedListAsync(request.PageNumber, request.PageSize);

            this.AddPaginationHeader(request, list, nameof(GetNodeList), null, filter, fields);

            return Ok(list);

        }

//        [HttpGet("shared", Name = nameof(GetSharedNodes))]
//        [HttpHead("shared")]
//        public IActionResult GetSharedNodes([FromQuery]PaginationRequest request, [FromQuery(Name = "public-only")]bool publicOnly = false)
//        {
//            var nodes = _dataProvider.GetSharedNodes(UserId.Value, request.PageNumber, request.PageSize, publicOnly);

//            this.AddPaginationHeader(request, nodes, nameof(GetNodeList), new RouteValueDictionary { { "public-only", publicOnly } });

//            return Ok(nodes);
//        }
        
        /// <summary>
        /// Download  zipped node content
        /// </summary>
        /// <param name="id">node Id (guid)</param>
        /// <returns></returns>
        [HttpGet("{id}.zip")]
        public async Task<IActionResult> Download(Guid id)
        {
            const string mongoDBProjection = "{Name:1, Blob:1, Type:1}";
            var entitiesForDownlaod = new Dictionary<Blob, string>();

            async Task FindChaildItemsAsync(dynamic node, BsonDocument f, string path = "")
            {
                if (((IDictionary<string, object>)node).ContainsKey("Blob"))
                {
                    if (!(node.Blob is null))
                    {
                        if (!entitiesForDownlaod.Keys.Any(k => k.Id == node.Blob._id))
                        {
                            entitiesForDownlaod.Add(
                                new Blob { Id = node.Blob._id, Bucket = node.Blob.Bucket },
                                $"{path}{node.Name}"
                            );
                        }
                    }
                }

                f.Set("ParentId", node._id);
                f.Remove("_id");

                path += node.Type != "Folder" ? $"{node.Name}_{node._id}" : node.Name;

                foreach (var chaildNode in await _nodes.Find(f).Project<dynamic>(mongoDBProjection).ToListAsync())
                    await FindChaildItemsAsync(chaildNode, f, $"{path}\\");
            }

            BsonDocument filter = new OrganizeFilter(UserId.Value).ById(id);

            var startNode = await _nodes.Find(filter)
                .Project<dynamic>(mongoDBProjection)
                .FirstOrDefaultAsync();

            await FindChaildItemsAsync(startNode, filter);

            foreach (var group in entitiesForDownlaod.GroupBy(ent => ent.Value).Where(g => g.Count() > 1))
            {
                int count = -1;
                foreach (var item in group)
                    if (++count > 0)
                        entitiesForDownlaod[item.Key] = $"{Path.GetDirectoryName(item.Value)}\\{Path.GetFileNameWithoutExtension(item.Value)}({count}){Path.GetExtension(item.Value)}";
            }

            return new FileCallbackResult(new MediaTypeHeaderValue("application/zip"), async (outputStream, _) =>
            {
                using (var zipArchive = new ZipArchive(new WriteOnlyStreamWrapper(outputStream), ZipArchiveMode.Create))
                {
                    foreach (var blob in entitiesForDownlaod)
                    {
                        var zipEntry = zipArchive.CreateEntry(blob.Value);
                        using (var zipStream = zipEntry.Open())
                        using (var stream = (await _blobStorage.GetFileAsync(blob.Key.Id, blob.Key.Bucket)).GetContentAsStream())
                            await stream.CopyToAsync(zipStream);
                    }
                }
            })
            {
                FileDownloadName = $"{id}_{DateTime.Now}.zip"
            };
        }

        [NonAction]
        public string CreatePageUri(PaginationRequest request, PaginationUriType uriType, string action, Guid? entityId = null, string filter = null, IEnumerable<string> fields = null)
        {
            int pageNumber = uriType == PaginationUriType.PreviousPage ? request.PageNumber - 1 : request.PageNumber + 1;

            return _urlHelper.Link(action, new RouteValueDictionary
            {
                { "id", entityId },
                { "pageSize",  request.PageSize },
                { "pageNumber", pageNumber },
                { "$filter", filter },
                { "$projection",  string.Join(",", fields ?? new string[] { }) }
            });
        }

        [NonAction]
        public string CreatePageUri(PaginationRequest request, PaginationUriType uriType, string action, RouteValueDictionary routeValueDictionary)
        {
            int pageNumber = uriType == PaginationUriType.PreviousPage ? request.PageNumber - 1 : request.PageNumber + 1;
            var routeValues = new RouteValueDictionary
            {
                { "pageSize",  request.PageSize },
                { "pageNumber", pageNumber }
            };

            foreach (var rv in routeValueDictionary)
                routeValues[rv.Key] = rv.Value;

            return _urlHelper.Link(action, routeValues);
        }

        [NonAction]
        private async Task<IEnumerable<BaseNode>> GetBreadcrumbs(BsonDocument node)
        {
            var filter = (BsonDocument)new OrganizeFilter(UserId.Value).ById(node["_id"].AsGuid);
            var parents = await _nodes.Aggregate().Match(filter)
                .GraphLookup(_nodes, "ParentId", "_id", "$ParentId", "Parent")
                .Unwind("Parent")
                .ReplaceRoot<BaseNode>("$Parent")
                .ToListAsync();

            parents.AsParallel()
                .ForAll(p => p.Name = WebUtility.UrlEncode(p.Name)?.Replace("+", "%20"));

            if (parents.Count() > 0)
            {
                var result = new List<BaseNode>();
                var it = parents.First(p => p.Id == node["ParentId"].AsGuid);
                result.Add(it);
                while (it.Id != UserId)
                {
                    it = parents.First(p => p.Id == it.ParentId);
                    result.Add(it);
                }

                return result;
            }
            else
            {
                return parents;
            }
        }

        [NonAction]
        private async Task<Responses.PagedList<dynamic>> GetNodesPagedList(Guid parentId, string types, PaginationRequest request, string status)
        {
            BsonDocument filter = new OrganizeFilter().ByParent(parentId);

            if (status != null)
                filter.Add("Status", status);

            if (!string.IsNullOrEmpty(types))
            {
                var bsonArray = new BsonArray();
                foreach (var type in types.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    bsonArray.Add(BsonRegularExpression.Create(new Regex($"^{type}$", RegexOptions.IgnoreCase)));
                }

                filter.Set("Type", new BsonDocument("$in", bsonArray));
            }

            var aggregation = GetNodeAggregation(filter);

            return await aggregation.ToPagedListAsync(request.PageNumber, request.PageSize); 
        }

        [NonAction]
        private IAggregateFluent<dynamic> GetNodeAggregation(BsonDocument filter)
        {
            return _nodes.Aggregate()
                .GraphLookup(_nodes, "ParentId", "_id", "$ParentId", "Parents")
                .Match(new BsonDocument("$or", new BsonArray(
                    new BsonDocument[] {
                        new BsonDocument("Parents", new BsonDocument("$size", 0)),
                        new BsonDocument("Parents.IsDeleted", new BsonDocument("$ne", true))
                    })))
                .Lookup<BsonDocument, BsonDocument>(nameof(AccessPermissions), "_id", "_id", nameof(AccessPermissions))
                .Unwind(nameof(AccessPermissions), new AggregateUnwindOptions<BsonDocument> { PreserveNullAndEmptyArrays = true })
                .Match(filter)
                .Project(@"{ 
                            Order: { $cond: { if: { $eq: [""$Type"", ""Folder""] }, then: 1, else: 100 } }
                            Version:1,
                            Status:1,
                            Type:1,
                            CreatedDateTime:1,
                            UpdatedDateTime:1,
                            Name:1,
                            OwnedBy:1,
                            CreatedBy:1,
                            Blob:1,
                            Images:1,
                            ParentId:1,
                            UpdatedBy:1,
                            SubType:1,
                            TotalRecords:1,
                            DisplayName:1,
                            FirstName:1,
                            LastName:1,
                            LoginName:1,
                            Email:1,
                            Avatar:1,
                            AccessPermissions:1
                        }")
                .Sort("{ Order:1, Name:1 }")
                .Project<dynamic>("{ Order: 0 }");
        }

        #region Get node by path
        //[HttpGet("", Name = nameof(Get))]
        //public async Task<IActionResult> Get(string path, string type, [FromQuery]PaginationRequest request)
        //{
        //    Log.Information($"Getting nodes from path '{path}'");

        //    var filter = (BsonDocument)new OrganizeFilter(UserId).ByParent(UserId);

        //    BsonDocument currentNodeIdDocument = new BsonDocument("_id", UserId);

        //    if (!string.IsNullOrEmpty(path))
        //    {
        //        foreach (var itemName in path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries))
        //        {
        //            var regex = BsonRegularExpression.Create(new Regex($"^{itemName}$", RegexOptions.IgnoreCase));
        //            filter.Set("Name", regex);

        //            currentNodeIdDocument = _nodes.Find(filter).Project(n => new { n.Id, n.ParentId, n.Name })
        //                .FirstOrDefault()
        //                ?.ToBsonDocument();

        //            if (currentNodeIdDocument == null)
        //                return NotFound($"Path '{path}' not found.");

        //            filter.Remove("Name");
        //            filter = filter.Set("ParentId", currentNodeIdDocument["_id"].AsGuid);
        //        }
        //    }

        //    return await GetNode(currentNodeIdDocument, type, request);
        //}

        //[NonAction]
        //private async Task<IActionResult> GetNode(BsonDocument nodeIdDocument, string types, PaginationRequest request)
        //{
        //    var filter = (BsonDocument)new OrganizeFilter(UserId).ByParent(nodeIdDocument["_id"].AsGuid);

        //    if (!string.IsNullOrEmpty(types))
        //    {
        //        var bsonArray = new BsonArray();
        //        foreach (var type in types.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        //        {
        //            bsonArray.Add(BsonRegularExpression.Create(new Regex($"^{type}$", RegexOptions.IgnoreCase)));
        //        }

        //        filter.Set("Type", new BsonDocument("$in", bsonArray));
        //    }

        //    var list = await _nodes.Aggregate().Match(filter)
        //        .Lookup<BaseNode, User, AggregatedNode>(_users, n => n.OwnedBy, u => u.Id, r => r.OwnedByUser).Unwind(i => i.OwnedByUser, new AggregateUnwindOptions<AggregatedNode> { PreserveNullAndEmptyArrays = true })
        //        .Lookup<AggregatedNode, User, AggregatedNode>(_users, n => n.UpdatedBy, u => u.Id, r => r.UpdatedByUser).Unwind(i => i.UpdatedByUser, new AggregateUnwindOptions<AggregatedNode> { PreserveNullAndEmptyArrays = true })
        //        .Project(f =>
        //        new
        //        {
        //            f.Id,
        //            f.Name,
        //            f.Type,
        //            f.Version,
        //            f.CreatedDateTime,
        //            f.UpdatedDateTime,
        //            f.Images,
        //            f.Blob,
        //            f.UpdatedByUser,
        //            f.OwnedByUser,
        //            Order = f.Type == "Folder" ? 1 : 100
        //        })
        //        .SortBy(p => p.Order)
        //        .ThenBy(p => p.Name)
        //        .Project(r =>
        //       new Node
        //       {
        //           Id = r.Id,
        //           Version = r.Version,
        //           Type = r.Type,
        //           CreatedDateTime = r.CreatedDateTime,
        //           UpdatedDateTime = r.UpdatedDateTime,
        //           Name = r.Name,
        //           OwnedBy = r.OwnedByUser,
        //           Blob = r.Blob,
        //           Images = r.Images,
        //           UpdatedBy = r.UpdatedByUser
        //       })
        //       .ToPagedListAsync(request.PageNumber, request.PageSize);

        //    this.AddPaginationHeader(request, list, nameof(Get), (nodeIdDocument is null || nodeIdDocument["_id"].IsBsonNull) ? null : nodeIdDocument["_id"]?.AsGuid);

        //    var Breadcrumbs = await GetBreadcrumbs(nodeIdDocument);
        //    Response.Headers["Access-Control-Expose-Headers"] += $", {breadcrumbsHeaderName}";
        //    Response.Headers.Add(breadcrumbsHeaderName, JsonConvert.SerializeObject(Breadcrumbs));

        //    return Ok(new
        //    {
        //        Id = (nodeIdDocument is null || nodeIdDocument["_id"].IsBsonNull) ? UserId : nodeIdDocument["_id"].AsGuid,
        //        Nodes = list
        //    });
        //}
        #endregion
    }
}