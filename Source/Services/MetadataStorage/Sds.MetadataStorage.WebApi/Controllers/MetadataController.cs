using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using MongoDB.Bson;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Serilog;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Sds.MetadataStorage.WebApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class MetadataController : Controller
    {
        IMongoDatabase _database;
        IMongoCollection<dynamic> _metadataCollection;

        public MetadataController(IMongoDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _metadataCollection = _database.GetCollection<dynamic>("Metadata");
        }

        // GET: api/metadata/{infoBoxType}?fileId=c1cc0000-222...
        [HttpGet("{infoBoxType}")]
        public async Task<IActionResult> Get(string infoBoxType, Guid? fileId)
        {
            Log.Information($"Getting metadta for {infoBoxType} infobox with FileId {fileId}");

            var filter = new BsonDocument("InfoBoxType", infoBoxType);
            if (infoBoxType.Equals("fields", StringComparison.InvariantCultureIgnoreCase))
            {
                if (fileId.HasValue)
                    filter.Add("FileId", fileId.Value);
                else
                {
                    Log.Error($"Request for fields metadata must provide fileId");
                    return BadRequest();
                }
            }

            var metaData = await _metadataCollection
                .Find(filter).SingleOrDefaultAsync();

            if (!(metaData is null))
            {
                Guid id = (Guid)((IDictionary<string, object>)metaData)["_id"];
                ((IDictionary<string, object>)metaData).Remove("_id");
                ((IDictionary<string, object>)metaData).Add("Id", id);

                return Ok(metaData);
            }
            else
            {
                Log.Error($"Metadta for {infoBoxType} infobox with FileId {fileId} not found.");
                return NotFound();
            }
        }

        [HttpGet("{entityId:guid}")]
        public async Task<IActionResult> Get(Guid entityId)
        {
            var metaData = await _metadataCollection.Find(new BsonDocument("_id", entityId)).FirstOrDefaultAsync();
            if (metaData is null)
                return NotFound();

            Guid id = (Guid)((IDictionary<string, object>)metaData)["_id"];
            ((IDictionary<string, object>)metaData).Remove("_id");
            ((IDictionary<string, object>)metaData).Add("Id", id);

            return Ok(metaData);
        }
    }
}
