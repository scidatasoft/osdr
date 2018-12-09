using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Sds.Imaging.Domain.Commands;
using Sds.Imaging.Domain.Models;
using Sds.Storage.Blob.Core;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Sds.Imaging.WebApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class ImagesController : Controller
    {
        private readonly IBusControl _bus;
        private readonly IBlobStorage _blobStorage;
        private readonly IMongoCollection<FileImages> _imagesMetaCollection;
        private string _userId => User.Claims.Where(c => c.Type.Contains("/nameidentifier")).FirstOrDefault() != null ? User.Claims.Where(c => c.Type.Contains("/nameidentifier")).First().Value : null;

        public ImagesController(IBusControl bus, IBlobStorage blobStorage, IMongoDatabase database)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
            _imagesMetaCollection = database?.GetCollection<FileImages>(nameof(FileImages)) ?? throw new ArgumentNullException(nameof(database));
        }

        // GET api/images/{id}
        /// <summary>
        /// Download an image
        /// </summary>
        /// <param name="id">Id of file to be returned</param>
        /// <response code="200">Returns binary data of image</response>
        /// <response code="404">File not found or request parameters incorrect</response>
        [HttpGet("{id}", Name = "GetImage")]
        public async Task<IActionResult> Get(Guid id)
        {
            Log.Information($"Getting file {id}");

            var imageInfo = await _imagesMetaCollection.Find(fi => fi.Images.Any(i => i.Id == id))
                   .Project(fi => new { Bucket = fi.Bucket, Image = fi.Images.FirstOrDefault(i => i.Id == id) })
                   .FirstOrDefaultAsync();

            if (imageInfo is null || !string.IsNullOrEmpty(imageInfo.Image.Exception))
            {
                string error = $"Image {id} not found. {imageInfo?.Image.Exception}";

                Log.Information(error);

                return NotFound(error);
            }

            var blobInfo = await _blobStorage.GetFileInfo(id, imageInfo.Bucket);

            if (blobInfo is null)
            {
                return AcceptedAtRoute("GetImage", new { id = id });
            }

            var blob = await _blobStorage.GetFileAsync(id, imageInfo.Bucket);

            return File(blob.GetContentAsStream(), blob.Info.ContentType, blob.Info.FileName);
        }

        // DELETE api/images/{id}
        /// <summary>
        /// Delete an image on server
        /// </summary>
        /// <param name="id">Blob id of file to be deleted</param>
        /// <param name="bucket">Bucket id</param>
        /// <response code="200">OK - image deleted</response>
        /// <response code="404">File not found or request parameters incorrect</response>
        [HttpDelete("{id}")]
        public async void Delete(Guid id, string bucket)
        {
            Log.Information($"Deleting image '{id}' in bucket '{bucket ?? _userId}'");

            await _bus.Publish<DeleteImage>(new
            {
                Id = id,
                Bucket = bucket ?? _userId,
                CorrelationId = Guid.Empty,
                UserId = new Guid(_userId)
            });
        }
    }
}
