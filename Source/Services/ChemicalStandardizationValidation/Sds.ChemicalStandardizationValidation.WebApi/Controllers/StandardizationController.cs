using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Sds.ChemicalStandardizationValidation.Domain.Commands;
using Sds.ChemicalStandardizationValidation.WebApi.Filters;
using Sds.Domain;
using Sds.Storage.Blob.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Sds.ChemicalStandardizationValidation.WebApi.Controllers
{
    [Produces("application/json")]
    [Route("api/standardization")]
    public class StandardizationController : Controller
    {
        private readonly IBusControl _bus;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BsonDocument> _standardizations;
        private readonly IBlobStorage _blobStorage;

        public StandardizationController(IBusControl bus, IMongoDatabase database, IBlobStorage blobStorage)
        {
            this._bus = bus ?? throw new ArgumentNullException(nameof(bus));
            this._database = database ?? throw new ArgumentNullException(nameof(database));
            this._blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
            this._standardizations = database.GetCollection<BsonDocument>("Standardizations");
        }

        public Guid UserId
        {
            get { return Guid.Parse(User.FindFirst("sub").Value); }
        }

        // GET: api/standardization/{id}
        [HttpGet("{id}", Name = "GetStandardization")]
        public async Task<IActionResult> Get(Guid id)
        {
            Log.Debug($"Get standardization with id ='{id}'");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
            var standardization = await _standardizations.Find(filter).FirstOrDefaultAsync();

            var standardized = "";

            if (standardization is null)
            {
                var blobInfo = _blobStorage.GetFileAsync(id, User.FindFirst("sub").Value);
                if (blobInfo is null)
                {
                    Log.Debug($"Standardization and mol file {id} not found.");
                    return NotFound();
                }
                else
                {
                    Log.Debug($"Standardization {id} is being processed.");
                    return new AcceptedAtRouteResult("GetStandardization", new { id = id }, null);
                }
            }
            else
            {
                using (var blob = await _blobStorage.GetFileAsync(id, User.FindFirst("sub").Value))
                {
                    StreamReader reader = new StreamReader(blob.GetContentAsStream());
                    standardized = reader.ReadToEnd();
                }
            }

            var issuesDocuments = standardization["Issues"];
            var issues = BsonSerializer.Deserialize<List<Issue>>(issuesDocuments.ToJson());

            return Ok (new {
                Issues = issues, 
                Standardized = standardized
            });
        }

        // POST: api/standardization
        [HttpPost]
        [DisableFormValueModelBinding]
        public async Task<IActionResult> Post()
        {
            string bucket = User.FindFirst("sub").Value;

            Log.Debug($"Request to standardization");

            if (!IsMultipartContentType(Request.ContentType))
                return new UnsupportedMediaTypeResult();

            Log.Debug($"POSTing files...");

            var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(Request.ContentType).Boundary);
            var reader = new MultipartReader(boundary.Value, Request.Body);

            MultipartSection section;

            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var contentDisposition = section.GetContentDispositionHeader();

                if (contentDisposition.IsFileDisposition())
                {
                    var fileSection = section.AsFileSection();

                    if (fileSection.FileName.ToLower().EndsWith(".mol"))
                    {
                        Log.Debug($"Saving file {fileSection.FileName}");

                        var blobId = await _blobStorage.AddFileAsync(fileSection.FileName, fileSection.FileStream, fileSection.Section.ContentType, bucket);

                        await _bus.Publish<Standardize>(new
                        {
                            Id = NewId.Next(),
                            Bucket = bucket,
                            BlobId = blobId,
                            CorrelationId = Guid.Empty,
                            UserId = UserId
                        });

                        return CreatedAtRoute("GetStandardization", new { id = blobId }, null);  //uploading only one file and return.
                    }
                }
            }
            return BadRequest();
        }

        // DELETE: api/standardization/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            Log.Debug($"Request to delete Standardization item with id ='{id}'");

            await _bus.Publish<DeleteStandardization>(new
            {
                Id = id,
                CorrelationId = Guid.Empty,
                UserId = UserId
            });

            return Ok();
        }

        private static bool IsMultipartContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}