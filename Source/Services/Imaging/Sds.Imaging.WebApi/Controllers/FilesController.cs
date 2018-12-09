using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using MongoDB.Driver;
using Newtonsoft.Json;
using Sds.Imaging.Domain.Commands;
using Sds.Imaging.Domain.Models;
using Sds.Imaging.WebApi.Filters;
using Sds.Imaging.WebApi.Requests;
using Sds.Storage.Blob.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Sds.Imaging.WebApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class FilesController : Controller
    {
        private readonly IBusControl _bus;
        private readonly IBlobStorage _blobStorage;
        private readonly IConfiguration _configuration;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<FileImages> _imagesMetaCollection;

        private string _bucket => User.Claims.Where(c => c.Type.Contains("/nameidentifier")).FirstOrDefault() != null ? User.Claims.Where(c => c.Type.Contains("/nameidentifier")).First().Value : null;

        public FilesController(IBlobStorage blobStorage, IBusControl bus, IMongoDatabase database, IConfiguration configuration)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _imagesMetaCollection = _database.GetCollection<FileImages>(nameof(FileImages));
        }

        /// <summary>
        /// Post file to server
        /// </summary>
        /// <param name="file">File to be uploaded</param>
        /// <response code="200">OK - file posted</response>
        [Route(""), HttpPost]
        [DisableFormValueModelBinding]
        public async Task<IActionResult> UploadFiles()
        {
            if (!IsMultipartContentType(Request.ContentType))
                return new UnsupportedMediaTypeResult();

            Log.Information($"POSTing files...");

            var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(Request.ContentType).Boundary);
            var reader = new MultipartReader(boundary.Value, Request.Body);

            MultipartSection section;

            IList<Image> imagesRequest = null;
            var result = new List<FileImages>();
            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var contentDisposition = section.GetContentDispositionHeader();
                if (contentDisposition.IsFormDisposition() && imagesRequest is null)
                {
                    var formSection = section.AsFormDataSection();
                    string formValue = await formSection.GetValueAsync();
                    imagesRequest = JsonConvert.DeserializeObject<IList<Image>>(formValue, new JsonSerializerSettings
                    {
                        Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                        {
                            args.ErrorContext.Handled = true;
                        }
                    });
                }
                if (contentDisposition.IsFileDisposition())
                {
                    var fileSection = section.AsFileSection();
                    if (string.IsNullOrEmpty(fileSection.FileName))
                    {
                        Log.Information($"Empty file section");
                        continue;
                    }
                    Log.Information($"Saving file {fileSection.FileName}");

                    var blobId = await _blobStorage.AddFileAsync(Path.GetFileName(fileSection.FileName), fileSection.FileStream, fileSection.Section.ContentType, _bucket);

                    if (imagesRequest is null || !imagesRequest.Any())
                    {
                        imagesRequest = new Image[]
                        {
                            new Image
                            {
                                Width = int.Parse(_configuration["DefaultImage:Width"]),
                                Format = _configuration["DefaultImage:Format"],
                                Height = int.Parse(_configuration["DefaultImage:Height"])
                            }
                        };
                    }

                    foreach (var requestedImage in imagesRequest)
                    {
                        requestedImage.Id = NewId.Next().ToGuid();

                        if (requestedImage.Width <= 0)
                            requestedImage.Width = int.Parse(_configuration["DefaultImage:Width"]);

                        if (requestedImage.Height <= 0)
                            requestedImage.Height = int.Parse(_configuration["DefaultImage:Height"]);
                    }

                    var fileImages = new FileImages
                    {
                        Id = blobId,
                        Images = imagesRequest,
                        Bucket = _bucket
                    };

                    //await _imagesMetaCollection.InsertOneAsync(fileImages);

                    foreach (var requestedImage in imagesRequest)
                    {
                        await _bus.Publish<GenerateImage>(new
                        {
                            Id = requestedImage.Id,
                            Bucket = _bucket,
                            BlobId = blobId,
                            Image = requestedImage,
                            UserId = new Guid(_bucket)
                        });
                    }

                    result.Add(fileImages);
                }
            }

            return Ok(result);
        }

        // POST api/files/id/images
        /// <summary>
        /// Generate images from an existing blob
        /// </summary>
        /// <param name="imagesRequest">Parameters of file to be created</param>
        /// <response code="200">OK - file posted</response>
        [Route("{blobId}/images"), HttpPost]
        public async Task<IActionResult> GenerateImage(Guid blobId, [FromBody]IEnumerable<ImageRequest> imagesRequest)
        {
            var fileInfo = await _blobStorage.GetFileInfo(blobId, _bucket);

            Log.Information($"Generation images for file '{blobId}'...");
            if (fileInfo is null)
            {
                var message = $"File '{blobId}' not found.";

                Log.Information(message);

                return NotFound(message);
            }

            var imageIds = new List<Guid>();
            foreach (var image in imagesRequest)
            {
                var doc = await _imagesMetaCollection.Find(d => d.Id == blobId).FirstOrDefaultAsync();
                var existingImage = doc?.Images.FirstOrDefault(i => i.Width == image.Width && i.Height == image.Height && string.Equals(i.Format, image.Format, StringComparison.OrdinalIgnoreCase));
                if (existingImage is null)
                {
                    var imageId = Guid.NewGuid();
                    imageIds.Add(imageId);
                    //var update = Builders<FileImages>.Update.AddToSet(fi => fi.Images, image);
                    //await _imagesMetaCollection.FindOneAndUpdateAsync(fd => fd.Id == blobId, update);

                    await _bus.Publish<GenerateImage>(new
                    {
                        Id = NewId.NextGuid(),
                        Bucket = _bucket,
                        BlobId = blobId,
                        Image = new Image
                        {
                            Id = imageId,
                            Width = image.Width,
                            Height = image.Height,
                            Format = image.Format
                        },
                        CorrelationId = Guid.Empty,
                        UserId = new Guid(_bucket)
                    });
                }
                else
                {
                    imageIds.Add(existingImage.Id);
                }
            }

            return Ok(imageIds);
        }

        // GET api/files/blobId/images
        /// <summary>
        /// Get image data
        /// </summary>
        /// <param name="id">Blob id</param>
        /// <response code="200">OK - image data returned</response>
        /// <response code="404">File not found or request parameters incorrect</response>
        [Route("{blobId}/images"), HttpGet]
        public async Task<IActionResult> GetImages(Guid blobId)
        {
            Log.Information($"Getting images for file '{blobId}'.");

            var doc = await _imagesMetaCollection.Find(d => d.Id == blobId).FirstOrDefaultAsync();
            if (doc is null)
            {
                var fileInfo = await _blobStorage.GetFileInfo(blobId, _bucket);
                if (fileInfo is null)
                {
                    var message = $"File '{blobId}' not found.";

                    Log.Information(message);

                    return NotFound(message);
                }
                else
                {
                    Log.Information($"File '{blobId}' exists");
                    return Accepted();
                }
            }

            return Ok(doc);
        }

        // DELETE api/files/{id}
        /// <summary>
        /// Delete image data
        /// </summary>
        /// <param name="id">Blob id</param>
        /// <response code="200">OK - file posted</response>
        /// <response code="404">File not found or request parameters incorrect</response>
        [HttpDelete("{id}")]
        public async void Delete(Guid id)
        {
            Log.Information($"Deleting file '{id}' in bucket '{_bucket}'");

            //await _commandSender.Send(new DeleteSource(id, _bucket, Guid.Empty, new Guid(_bucket)));

            await _bus.Publish<DeleteSource>(new
            {
                Id = id,
                Bucket = _bucket,
                CorrelationId = Guid.Empty,
                UserId = new Guid(_bucket)
            });
        }

        private static bool IsMultipartContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
