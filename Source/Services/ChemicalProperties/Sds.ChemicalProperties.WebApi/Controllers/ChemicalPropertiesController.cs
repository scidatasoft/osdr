using System;
using Microsoft.AspNetCore.Mvc;
using Sds.ChemicalProperties.Domain.Commands;
using Sds.ChemicalProperties.WebApi.Dto;
using Serilog;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading.Tasks;
using Sds.ChemicalProperties.Domain.Models;
using Sds.ChemicalProperties.WebApi.Filters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Sds.Storage.Blob.Core;
using Microsoft.AspNetCore.Authorization;
using MassTransit;

namespace Sds.ChemicalProperties.WebApi.Controllers
{
    [Route("api/chemical-properties")]
    [Authorize]
    public class ChemicalPropertiesController : Controller
    {
        private readonly IBusControl _bus;
        private readonly IMongoDatabase _database;
        private readonly IBlobStorage _blobStorage;
        private readonly IMongoCollection<CalculatedPropertiesResponse> _collection;
        public ChemicalPropertiesController(IBusControl bus, IMongoDatabase database, IBlobStorage blobStorage)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
            _collection = _database.GetCollection<CalculatedPropertiesResponse>(nameof(ChemicalProperties));
        }

        public Guid UserId
        {
            get { return Guid.Parse(User.FindFirst("sub").Value); }
        }

        // GET api/chemicalproperties/{id}
        [HttpGet("{id}", Name = "Get")]
        public async Task<IActionResult> Get(Guid id)
        {
            string bucket = User.FindFirst("sub").Value;

            Log.Information($"Get ChemicalProperties with id ='{id}'");
            var filter = Builders<CalculatedPropertiesResponse>.Filter.Eq(p => p.Id, id);
            var calculatedProperties = await _collection.Find(filter).FirstOrDefaultAsync();

            if (calculatedProperties is null)
            {
                var blobInfo = await _blobStorage.GetFileInfo(id, bucket);
                if (blobInfo is null)
                {
                    Log.Information($"ChemicalProperties and mol file {id} not found.");
                    return NotFound();
                }
                else
                {
                    Log.Information($"ChemicalProperties {id} is being processed.");
                    return new AcceptedAtRouteResult("Get", new { id = id }, null);
                }
            }

            return Ok(calculatedProperties);
        }

        // POST api/chemicalproperties
        [HttpPost]
        [DisableFormValueModelBinding]
        public async Task<IActionResult> Post()
        {
            string bucket = UserId.ToString();

            Log.Information($"Request to calculate ChemicalProperties");

            if (!IsMultipartContentType(Request.ContentType))
                return new UnsupportedMediaTypeResult();

            Log.Information($"POSTing files...");

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
                        Log.Information($"Saving file {fileSection.FileName}");

                        var id = await _blobStorage.AddFileAsync(fileSection.FileName, fileSection.FileStream, fileSection.Section.ContentType, bucket);

                        //await _commandSender.Send(new CalculateChemicalProperties(Guid.NewGuid(), Guid.NewGuid(), UserId, bucket, id));

                        return CreatedAtRoute("Get", new { id = id }, null);  //uploading only one file and return.
                    }
                }
            }

            return BadRequest();
        }

        // DELETE api/chemicalproperties/{id}
        [HttpDelete("{id}")]
        public IActionResult Delete(Guid id)
        {
            Log.Information($"Request to delete ChemicalProperties with id ='{id}'");
            //_commandSender.Send(new DeleteChemicalProperties(id, Guid.NewGuid(), UserId));

            return Ok();
        }

        private static bool IsMultipartContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
