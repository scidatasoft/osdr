using CQRSlite.Events;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Sds.Osdr.Generic.Domain.Commands.Files;
using Sds.Osdr.Generic.Domain.Commands.Folders;
using Sds.Osdr.Generic.Domain.ValueObjects;
using Sds.Osdr.MachineLearning.Domain.Commands;
using Sds.Osdr.RecordsFile.Domain.Commands.Records;
using Sds.Osdr.WebApi.DataProviders;
using Sds.Osdr.WebApi.Extensions;
using Sds.Osdr.WebApi.Filters;
using Sds.Osdr.WebApi.Requests;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Sds.Osdr.WebApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class EntitiesController : MongoDbController, IPaginationController
    {
        private IUrlHelper _urlHelper;
        private IBusControl _bus;
        private EventStore.IEventStore _eventStorage;
        Regex _invalidName = new Regex($"^([{Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()))}]|con|aux|com[1-9]|lpt[1-9]|nul|prn|^$)$", RegexOptions.IgnoreCase);
        readonly IOrganizeDataProvider _dataProvider;
        private IMongoCollection<BsonDocument> _accessPermissions;
        readonly string[] acceptableEntitites = new[] { "files", "folders", "records", "users", "models" };

        public EntitiesController(IMongoDatabase database, IUrlHelper urlHelper, IBusControl bus, IOrganizeDataProvider dataProvider, EventStore.IEventStore eventStorage)
            : base(database)
        {
            _urlHelper = urlHelper ?? throw new ArgumentNullException(nameof(urlHelper));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(IOrganizeDataProvider));
            _eventStorage = eventStorage ?? throw new ArgumentNullException(nameof(eventStorage));
            _accessPermissions = Database.GetCollection<BsonDocument>("AccessPermissions");
        }

        /// <summary>
        /// Get single entity
        /// api/entities/files/3df6e978-b22b-4e98-a34a-332a89e06fcd?$projection=name,totalRecords
        /// </summary>
        /// <param name="type">entity type (Files, Folders, Records ...)</param>
        /// <param name="id">entity Id (guid)</param>
        /// <param name="fields">Field list</param>
        /// <returns></returns>
        [HttpGet("{type}/{id}", Name = "GetSingleEntity")]
        [AllowSharedContent]
        public async Task<IActionResult> Get(string type, Guid id,
            [FromQuery(Name = "$projection"), ModelBinder(BinderType = typeof(ArrayModelBinder))]IEnumerable<string> fields)
        {
            if (!acceptableEntitites.Contains(type.ToLower()))
                return BadRequest("Unacceptable entity type");

            Log.Information($"Getting entity '{id}' from collection '{type}'");

            BsonDocument filter = new OrganizeFilter().ById(id);

            var result = await Database.GetCollection<dynamic>(type.ToPascalCase()).Aggregate()
                .Match(filter)
                .Project<dynamic>((fields ?? new string[] { }).ToMongoDBProjection())
                .FirstOrDefaultAsync();

            if (result is null)
                return NotFound();

            return Ok(result);
        }
        
        [AllowAnonymous]
        [HttpGet("{type}/public", Name = nameof(GetEntitiesPublic))]
        public async Task<IActionResult> GetEntitiesPublic(string type, [FromQuery(Name = "$filter")]string filter,
            [FromQuery]PaginationRequest request,
            [FromQuery(Name = "$projection"), ModelBinder(BinderType = typeof(ArrayModelBinder))]IEnumerable<string> fields)
        {
            //IMongoCollection<BsonDocument> collection = null;
            type = type.ToPascalCase();
            var foreignKeyForLookup = "";
            
            switch (type)
            {
                case "Records": foreignKeyForLookup = "FileId"; break;
                case "Files": foreignKeyForLookup = "_id"; break;
                case "Folders": foreignKeyForLookup = "_id"; break;
                case "Models": foreignKeyForLookup = "_id"; break;
            }
            
            if (string.IsNullOrEmpty(type))
            {
                return BadRequest();
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
                .Lookup(type, "_id", foreignKeyForLookup, "Parents")
                .Unwind("Parents")
                .ReplaceRoot(new BsonValueAggregateExpressionDefinition<BsonDocument, BsonDocument>("$Parents"))
                .Match(filterDocument)
                .Project<dynamic>(projection);
            
            var list = await aggregation.ToPagedListAsync(request.PageNumber, request.PageSize);

            this.AddPaginationHeader(request, list, nameof(GetEntitiesPublic), null, filter, fields);
            
            return Ok(list);
        }

        [HttpGet("{type}/me", Name = nameof(GetEntityListMe))]
        [HttpHead("{type}/me")]
        public async Task<IActionResult> GetEntityListMe(string type,
            [FromQuery(Name = "$filter")]string filter,
            [FromQuery]PaginationRequest request,
            [FromQuery(Name = "$projection"), ModelBinder(BinderType = typeof(ArrayModelBinder))]IEnumerable<string> fields)
        {
            return await GetEntityList(type, filter, request, fields);
        }

        /// <summary>
        /// Get entity list
        /// api/entities/files/?$filter=subType eq 'Model' and status eq 'Processing' and StatusInt eq 3
        /// </summary>
        /// <param name="type">entity type (Files, Folders, Records ...)</param>
        /// <param name="request">pagination requst(pageSize, pageNumber)</param>
        /// <param name="fields">Field list</param>
        /// <param name="filter">filter string</param>
        /// <returns></returns>
        [HttpGet("{type}", Name = nameof(GetEntityList))]
        [HttpHead("{type}")]
        public async Task<IActionResult> GetEntityList(string type,
            [FromQuery(Name = "$filter")]string filter,
            [FromQuery]PaginationRequest request,
            [FromQuery(Name = "$projection"), ModelBinder(BinderType = typeof(ArrayModelBinder))]IEnumerable<string> fields)
        {
            Log.Information($"Getting entities fields '{string.Join(",", fields ?? new[] { "all" }) }' from collection '{type}' for userId '{UserId}' with filter: \"{filter}\"");

            BsonDocument filterDocument = null;
            try
            {
                filterDocument = new OrganizeFilter(UserId.Value).ByQueryString(filter);
            }
            catch(Exception e)
            {
                Log.Error($"Filter error '{filter}' {e}");
                return BadRequest();
            }
            var result = await Database.GetCollection<BsonDocument>(type.ToPascalCase())
                .Find(filterDocument)
                .Project<dynamic>(fields?.ToMongoDBProjection())
                .ToPagedListAsync(request.PageNumber, request.PageSize);

            this.AddPaginationHeader(request, result, nameof(GetEntityList), null, filter, fields);

            return Ok(result);
        }

        [HttpGet("{type}/{id}/{*propertyPath}")]
        public async Task<IActionResult> Get(string type, Guid id, string propertyPath)
        {
            Log.Information($"Getting entity '{id}' from collection '{type}' for userId '{UserId}'");

            var result = await Database.GetCollection<dynamic>(type.ToPascalCase())
                .Find((BsonDocument)new OrganizeFilter(UserId.Value).ById(id))
                .FirstOrDefaultAsync();

            if (result is null)
            {
                Log.Error($"Entity '{id}' not found in collection '{type}' for userId '{UserId}'");
                return NotFound();
            }

            if (!string.IsNullOrEmpty(propertyPath))
            {
                foreach (var property in propertyPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    result = (((IDictionary<string, object>)result))[property.ToPascalCase()];
                }
            }

            return Ok(result);
        }

        /// <summary>
        /// Get stream events list for entity from EventStore
        /// </summary>
        /// <param name="type">type for inner node</param>
        /// <param name="id">node id or 'me' for root user node</param>
        /// <param name="request">pagination request</param>
        /// <returns></returns>
        [AllowSharedContent]
        [HttpGet("{type}/{id}/stream", Name = nameof(GetAggregateStream))]
        public async Task<IActionResult> GetAggregateStream(string type, Guid id, [FromQuery]ItemsSequenceRequest request)
        {
            var events = await _eventStorage.ReadEventsBackwardAsync(id, request.Start, request.Count);
            var list = new PagedList<IEvent>(events, events.Count(), 1, request.Count).Select(e=> new { Event = e , Namespace = e.GetType().Namespace, Name = e.GetType().Name });

            return Ok(list);
        }

        [HttpGet("{type}/{id}/images/{imageId}")]
        [AllowSharedContent]
        public async Task<IActionResult> GetImage(string type, Guid id, Guid imageId, [FromQuery(Name = "content-disposition")]string contentDisposition = "inline")
        {
            Log.Information($"Getting image '{imageId}' from entity '{id}' from collection '{type}' for userId '{UserId}'");

            if (string.Equals(type, "folder", StringComparison.InvariantCultureIgnoreCase))
                return BadRequest();

            var image = await _dataProvider.GetEntityImageAsync(type, id, imageId);

            if (image is null)
            {
                Log.Error($"Entity '{id}' not found in collection '{type}' for userId '{UserId}'");
                return NotFound();
            }

            if (!new string[] { "inline", "attachment" }.Contains(contentDisposition))
                contentDisposition = "inline";

            var cd = new ContentDispositionHeaderValue(contentDisposition)
            {
                FileName = image.Info.FileName
            };

            Response.Headers[HeaderNames.ContentDisposition] = cd.ToString();

            return File(image.GetContentAsStream(), image.Info.ContentType);
        }

        [HttpGet("{type}/{id}/blobs/{blobId}")]
        [AllowSharedContent]
        public async Task<IActionResult> GetBlob(string type, Guid id, Guid blobId, [FromQuery(Name = "content-disposition")]string contentDisposition = "inline")
        {
            Log.Information($"Getting blob '{blobId}' from entity '{id}' from collection '{type}' for userId '{UserId}'");

            if (string.Equals(type, "folder", StringComparison.InvariantCultureIgnoreCase))
                return BadRequest();

            var entityBlob = await _dataProvider.GetEntityBlobAsync(type, id, blobId);

            if (entityBlob.blob is null)
            {
                Log.Error($"Entity '{id}' not found in collection '{type}' for userId '{UserId}'");
                return NotFound();
            }

            if (!new string[] { "inline", "attachment" }.Contains(contentDisposition))
                contentDisposition = "inline";

            var cd = new ContentDispositionHeaderValue(contentDisposition)
            {
                FileName = entityBlob.osdrFileName
            };

            Response.Headers[HeaderNames.ContentDisposition] = cd.ToString();

            return File(entityBlob.blob.GetContentAsStream(), entityBlob.blob.Info.ContentType);
        }
        
        [HttpGet("{type}/{id}/blobs/{blobId}/info", Name = nameof(GetBlobMetadataInfo))]
        [AllowAnonymous]
        public async Task<IActionResult> GetBlobMetadataInfo(string type, Guid id, Guid blobId, [FromQuery(Name = "content-disposition")]string contentDisposition = "inline")
        {
            Log.Information($"Getting blob '{blobId}' from entity '{id}' from collection '{type}' for userId '{UserId}'");

            if (string.Equals(type, "folder", StringComparison.InvariantCultureIgnoreCase))
                return BadRequest();

            var entityBlob = await _dataProvider.GetEntityBlobAsync(type, id, blobId);

            if (entityBlob.blob is null)
            {
                Log.Error($"Entity '{id}' not found in collection '{type}' for userId '{UserId}'");
                return NotFound();
            }

            var metadata = entityBlob.blob.Info.Metadata;

            return Ok(metadata);
        }

        /// <summary>
        /// Creating folder
        /// </summary>
        /// <param name="request">Info about new folder (name, parentId )</param>
        /// <returns></returns>
        /// <response code="202">folder creation started</response>
        /// <response code="400">folder name is not valid</response>
        [ProducesResponseType(202)]
        [ProducesResponseType(400)]
        [HttpPost("folders")]
        public async Task<IActionResult> Create([FromBody]CreateFolderRequest request)
        {
            if (_invalidName.IsMatch(request.Name?.Trim() ?? string.Empty))
            {
                Log.Error($"Invalid folder name {request.Name}");
                return BadRequest();
            }

            Guid folderId = Guid.NewGuid();
            Guid correlationId = Guid.Empty;

            Log.Information($"Creating folder with Id: {folderId}, ParentId: {request.ParentId}, Name: {request.Name}, UserId: {UserId}");

            await _bus.Publish<CreateFolder>(new
            {
                Id = folderId,
                UserId = UserId,
                Name = request.Name,
                ParentId = request.ParentId
            });

            return AcceptedAtRoute("GetSingleEntity", new { type = "folders", id = folderId }, null);
        }

        /// <summary>
        /// Update File
        /// </summary>
        /// <param name="id">File identifier</param>
        /// <param name="request">Request with json patch object</param>
        /// <param name="version">Version object</param>
        /// <returns></returns>
        /// <response code="202">file updating started</response>
        /// <response code="400">new file name is not valid</response>
        [ProducesResponseType(202)]
        [ProducesResponseType(400)]
        [HttpPatch("files/{id}")]
        public async Task<IActionResult> PatchFile(Guid id, [FromBody]JsonPatchDocument<UpdatedEntityData> request, int version)
        {
            BsonDocument filter = new OrganizeFilter(UserId.Value).ById(id);

            var permissions = await Database.GetCollection<dynamic>("Files").Aggregate().Match(filter)
                .Lookup<BsonDocument, BsonDocument>(nameof(AccessPermissions), "_id", "_id", nameof(AccessPermissions))
                .Unwind(nameof(AccessPermissions), new AggregateUnwindOptions<BsonDocument> { PreserveNullAndEmptyArrays = true })
                .Project("{AccessPermissions:1}")
                .FirstOrDefaultAsync();

            if (permissions is null)
                return NotFound();

            var file = new UpdatedEntityData() { Id = id };

            if (permissions.Contains(nameof(AccessPermissions)))
                file.Permissions = BsonSerializer.Deserialize<AccessPermissions>(permissions[nameof(AccessPermissions)].AsBsonDocument);

            request.ApplyTo(file);

            if (!string.IsNullOrEmpty(file.Name))
            {
                if (_invalidName.IsMatch(file.Name?.Trim() ?? string.Empty))
                {
                    Log.Error($"Invalid file name {file.Name}");
                    return BadRequest();
                }

                Log.Information($"Renaming file {id} to { file.Name}");
                await _bus.Publish<RenameFile>(new
                {
                    Id = id,
                    UserId = UserId.Value,
                    NewName = file.Name.Trim(),
                    ExpectedVersion = version
                });
            }

            if (file.ParentId != Guid.Empty)
            {
                Log.Information($"Move file {id} to {file.ParentId}");
                await _bus.Publish<MoveFile>(new
                {
                    Id = id,
                    UserId = UserId.Value,
                    NewParentId = file.ParentId,
                    ExpectedVersion = version
                });
            }

            if (request.Operations.Any(o => o.path.Contains("/Permissions")))
            {
                Log.Information($"Grant access to file {id}");
                await _bus.Publish<Generic.Domain.Commands.Files.GrantAccess>(new
                {
                    Id = id,
                    UserId = UserId.Value,
                    file.Permissions
                });
            }

            return Accepted();
        }

        /// <summary>
        /// Update Folder
        /// </summary>
        /// <param name="id">Folder identifier</param>
        /// <param name="request">Request with json patch object</param>
        /// <param name="version">Version object</param>
        /// <returns></returns>
        /// <response code="202">folder updating started</response>
        /// <response code="400">new folder name is not valid</response>
        [ProducesResponseType(202)]
        [ProducesResponseType(400)]
        [HttpPatch("folders/{id}")]
        public async Task<IActionResult> PatchFolder(Guid id, [FromBody]JsonPatchDocument<UpdatedEntityData> request, int version)
        {
            Log.Information($"Updating folder {id}");

            BsonDocument filter = new OrganizeFilter(UserId.Value).ById(id);

            var folderToUpdate = await Database.GetCollection<dynamic>("Folders").Aggregate().Match(filter)
                .Lookup<BsonDocument, BsonDocument>(nameof(AccessPermissions), "_id", "_id", nameof(AccessPermissions))
                .Unwind(nameof(AccessPermissions), new AggregateUnwindOptions<BsonDocument> { PreserveNullAndEmptyArrays = true })
                .Project("{AccessPermissions:1}")
                .FirstOrDefaultAsync();

            if (folderToUpdate is null)
                return NotFound();

            var folder = new UpdatedEntityData() { Id = id };

            if (folderToUpdate.Contains(nameof(AccessPermissions)))
                folder.Permissions = BsonSerializer.Deserialize<AccessPermissions>(folderToUpdate[nameof(AccessPermissions)].AsBsonDocument);

            request.ApplyTo(folder);

            if (!string.IsNullOrEmpty(folder.Name))
            {
                if (_invalidName.IsMatch(folder.Name?.Trim() ?? string.Empty))
                {
                    Log.Error($"Invalid file name {folder.Name}");
                    return BadRequest();
                }

                Log.Information($"Rename folder {id} to { folder.Name}");

                await _bus.Publish<RenameFolder>(new
                {
                    Id = id,
                    UserId = UserId,
                    NewName = folder.Name.Trim(),
                    ExpectedVersion = version
                });
            }

            if (folder.ParentId != Guid.Empty)
            {
                Log.Information($"Move folder {id} to {folder.ParentId}");

                await _bus.Publish<MoveFolder>(new
                {
                    Id = id,
                    UserId = UserId,
                    NewParentId = folder.ParentId,
                    ExpectedVersion = version
                });
            }

            if (request.Operations.Any(o => o.path.Contains("/Permissions")))
            {
                Log.Information($"Grant access to folder {id}");
                await _bus.Publish<Generic.Domain.Commands.Folders.GrantAccess>(new
                {
                    Id = id,
                    UserId,
                    folder.Permissions
                });
            }

            return Accepted();
        }

        /// <summary>
        /// Update model
        /// </summary>
        /// <param name="id">Model identifier</param>
        /// <param name="request">Request with json patch object</param>
        /// <param name="version">Version object</param>
        /// <returns></returns>
        /// <response code="202">model updating started</response>
        /// <response code="400">new model name is not valid</response>
        [ProducesResponseType(202)]
        [ProducesResponseType(400)]
        [HttpPatch("models/{id}")]
        public async Task<IActionResult> PatchModel(Guid id, [FromBody]JsonPatchDocument<UpdatedEntityData> request, int version)
        {
            Log.Information($"Updating model {id}");

            BsonDocument filter = new OrganizeFilter(UserId.Value).ById(id);

            var modelToUpdate = await Database.GetCollection<dynamic>("Models").Aggregate().Match(filter)
                .Lookup<BsonDocument, BsonDocument>(nameof(AccessPermissions), "_id", "_id", nameof(AccessPermissions))
                .Unwind(nameof(AccessPermissions), new AggregateUnwindOptions<BsonDocument> { PreserveNullAndEmptyArrays = true })
                .Project("{AccessPermissions:1}")
                .FirstOrDefaultAsync();

            if (modelToUpdate is null)
                return NotFound();

            var model = new UpdatedEntityData() { Id = id };

            if (modelToUpdate.Contains(nameof(AccessPermissions)))
                model.Permissions = BsonSerializer.Deserialize<AccessPermissions>(modelToUpdate[nameof(AccessPermissions)].AsBsonDocument);

            request.ApplyTo(model);

            if (!string.IsNullOrEmpty(model.Name))
            {
                if (_invalidName.IsMatch(model.Name?.Trim() ?? string.Empty))
                {
                    Log.Error($"Invalid model name {model.Name}");
                    return BadRequest();
                }

                Log.Information($"Rename model {id} to { model.Name}");

                await _bus.Publish<UpdateModelName>(new
                {
                    Id = id,
                    UserId = UserId,
                    ModelName = model.Name.Trim(),
                    ExpectedVersion = version
                });
            }

            if (model.ParentId != Guid.Empty)
            {
                Log.Information($"Move model {id} to {model.ParentId}");

                await _bus.Publish<MoveModel>(new
                {
                    Id = id,
                    UserId = UserId,
                    NewParentId = model.ParentId,
                    ExpectedVersion = version
                });
            }

            if (request.Operations.Any(o => o.path.Contains("/Permissions")))
            {
                Log.Information($"Grant access to model {id}");
                await _bus.Publish<MachineLearning.Domain.Commands.GrantAccess>(new
                {
                    Id = id,
                    UserId,
                    model.Permissions
                });
            }

            return Accepted();
        }

        [HttpPatch("records/{id}")]
        public async Task<IActionResult> UpdateRecord(Guid id, int version, [FromBody]JsonPatchDocument<dynamic> request)
        {
            BsonDocument filter = new OrganizeFilter(UserId.Value).ById(id);

            var record = await Database.GetCollection<dynamic>("Records").Aggregate().Match(filter)
                .Lookup<BsonDocument, BsonDocument>(nameof(AccessPermissions), "_id", "_id", nameof(AccessPermissions))
                .Unwind(nameof(AccessPermissions), new AggregateUnwindOptions<BsonDocument> { PreserveNullAndEmptyArrays = true })
                .Project<dynamic>("{AccessPermissions:1, Properties:1}")
                .FirstOrDefaultAsync();

            if (record is null)
                return NotFound();

            if (!((IDictionary<string, object>)record).ContainsKey("Permissions"))
                ((IDictionary<string, object>)record).Add("Permissions", new AccessPermissions
                {
                    Users = new HashSet<Guid>(),
                    Groups = new HashSet<Guid>()
                });

            request.ApplyTo(record);
            if (request.Operations.Select(o => o.path).Any(p => p.Equals("/Properties/Fields")))
            {
                await _bus.Publish<UpdateFields>(new
                {
                    Id = id,
                    UserId,
                    ExpectedVersion = version,
                    record.Properties.Fields
                });
            }

            if (request.Operations.Any(o => o.path.Contains("/Permissions")))
            {
                Log.Information($"Grant access to folder {id}");
                await _bus.Publish<RecordsFile.Domain.Commands.Records.SetAccessPermissions>(new
                {
                    Id = id,
                    UserId,
                    record.Permissions
                });
            }

            return AcceptedAtRoute("GetSingleEntity", new { type = "records", id = id }, null);
        }

        /// <summary>
        /// Get metadata for infobox
        /// </summary>
        /// <param name="entityType">Entity type (files, folders, records)</param>
        /// <param name="id">Entity id</param>
        /// <param name="infoboxType">Infobox tyoe</param>
        /// <returns>Metadata object</returns>
        [HttpGet("{type}/{id}/metadata/{infoboxType}")]
        [AllowSharedContent]
        public async Task<IActionResult> GetInfoboxMetadata(string entityType, Guid id, string infoboxType)
        {
            var metdata = await _dataProvider.GetInfoboxMetadataAsync(entityType, id, infoboxType);
            if (metdata is null)
                return NotFound();

            return Ok(metdata);
        }

        /// <summary>
        /// Get metadata for infobox
        /// </summary>
        /// <param name="entityType">Entity type (files, folders, records)</param>
        /// <param name="id">Entity id</param>
        /// <returns>Metadata object</returns>
        [HttpGet("{type}/{id}/metadata")]
        [AllowSharedContent]
        public async Task<IActionResult> GetMetadata(string entityType, Guid id)
        {
            var metdata = await _dataProvider.GetEntityMetadataAsync(entityType, id);
            if (metdata is null)
                return NotFound();

            return Ok(metdata);
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

            return _urlHelper.Link(action, routeValueDictionary);
        }
    }
}