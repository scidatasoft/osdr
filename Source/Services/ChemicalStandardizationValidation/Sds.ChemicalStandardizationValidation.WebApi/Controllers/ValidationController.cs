using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Sds.ChemicalStandardizationValidation.Domain.Commands;
using Sds.ChemicalStandardizationValidation.Domain.Models;
using Sds.ChemicalStandardizationValidation.WebApi.Filters;
using Sds.Storage.Blob.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sds.ChemicalStandardizationValidation.WebApi.Controllers
{
    [Produces("application/json")]
    [Route("api/validation")]
    public class ValidationController : Controller
    {
        private readonly IBusControl _bus;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BsonDocument> _validations;
        private readonly IBlobStorage _blobStorage;

        public ValidationController(IBusControl bus, IMongoDatabase database, IBlobStorage blobStorage)
        {
            this._bus = bus ?? throw new ArgumentNullException(nameof(bus));
            this._database = database ?? throw new ArgumentNullException(nameof(database));
            this._blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
            this._validations = database.GetCollection<BsonDocument>("Validations");
        }

        public Guid UserId
        {
            get { return Guid.Parse(User.FindFirst("sub").Value); }
        }

        // GET: api/validation/{id}
        [HttpGet("{id}", Name = "GetValidation")]
        public async Task<IActionResult> Get(Guid id)
        {
            Log.Debug($"Get validation with id ='{id}'");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
            var validation = await _validations.Find(filter).FirstOrDefaultAsync();

            if (validation is null)
            {
                Log.Debug($"Validation {id} is being processed.");
                return new AcceptedAtRouteResult("GetValidation", new { id = id }, null);
            }
            var issuesDocuments = validation["Issues"];
            var issues = BsonSerializer.Deserialize<List<Issue>>(issuesDocuments.ToJson());

            return Ok(new
            {
                Issues = issues,
            });
        }

        // POST: api/validation
        [HttpPost]
        [DisableFormValueModelBinding]
        public async Task<IActionResult> PostAsync()
        {
            //string bucket = "Validations"; //TODO: change to UserId
            string bucket = User.FindFirst("sub").Value;
            Log.Debug($"Request to Validation...");

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

                    Log.Debug($"Saving file {fileSection.FileName}");

                    var blobId = await _blobStorage.AddFileAsync(fileSection.FileName, fileSection.FileStream, fileSection.Section.ContentType, bucket);

                    await _bus.Publish<Validate>(new
                    {
                        Id = NewId.Next(),
                        Bucket = bucket,
                        BlobId = blobId,
                        CorrelationId = Guid.Empty,
                        UserId = UserId
                    });

                    return CreatedAtRoute("GetValidation", new { id = blobId }, null);  //uploading only one file and return.
                }
            }
            return BadRequest();
        }

        // DELETE: api/validation/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            Log.Debug($"Request to delete Validation item with id = '{id}'");

            await _bus.Publish<DeleteValidation>(new
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
