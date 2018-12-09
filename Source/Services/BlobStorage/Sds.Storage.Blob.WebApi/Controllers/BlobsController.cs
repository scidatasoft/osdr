using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Serilog;
using Sds.Storage.Blob.Core;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.WebUtilities;
using System;
using Sds.Blob.Storage.WebApi.Filters;
using MassTransit;
using Sds.Storage.Blob.Events;
using Microsoft.AspNetCore.Authorization;
using Sds.Storage.Blob.Domain.Dto;
using System.IO;
using System.Linq;

namespace Sds.Blob.Storage.WebApi
{
    [Route("api/[controller]")]
    [Authorize]
    public class BlobsController : Controller
    {
        private readonly IBlobStorage _blobStorage;
        private readonly IBusControl _bus;

        private Guid? UserID
        {
            get
            {
                return User.Claims.Where(c => c.Type.Contains("/nameidentifier")).FirstOrDefault() != null ? (Guid?)Guid.Parse(User.Claims.Where(c => c.Type.Contains("/nameidentifier")).First().Value) : null;
                //Guid.TryParse(sub, out userId) ? (Guid?)userId : null;
            }
        }

        public BlobsController(IBlobStorage blobStorage, IBusControl bus)
        {
            this._blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
            this._bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }
        
        /// <summary>
        /// Download blob by blob id
        /// </summary>
        /// <remarks>
        /// Request a binary representation of a file on the server. For example,
        /// you can download an image if you know its Blob id. Your user id is used as the
        /// bucket id.
        /// </remarks>
        /// <example> This is a first example </example>
        /// <param name="bucket">Bucket Id</param>
        /// <param name="id">Blob Id</param>
        /// <param name="contentDisposition">Content disposition: inline or attachment</param>
        /// <returns>A file in binary form</returns>
        /// <response code="200">OK - Download sent</response>
        /// <example> This is a second example </example>
        /// <response code="404">File not found or request parameters incorrect</response>
        [HttpGet("{bucket}/{id}")]
        public async Task<IActionResult> Get(string bucket, Guid id, [FromQuery(Name = "content-disposition")]string contentDisposition = "inline")
        {
            Log.Information($"Getting file {id}");
            var blobInfo = await _blobStorage.GetFileInfo(id, bucket);

            if (blobInfo == null)
            {
                Log.Information($"File {id} not found.");

                return new NotFoundObjectResult("File not found.");
            }

            var blob = await _blobStorage.GetFileAsync(id, bucket);

            if (!new string[] { "inline", "attachment" }.Contains(contentDisposition))
            {
                contentDisposition = "inline";
            }

            var cd = new ContentDispositionHeaderValue(contentDisposition)
            {
                FileName = blob.Info.FileName
            };

            Response.Headers[HeaderNames.ContentDisposition] = cd.ToString();

            return File(blob.GetContentAsStream(), blob.Info.ContentType);
        }

        /// <summary>
        /// Upload blob
        /// </summary>
        /// <remarks>
        /// You can send a file to the server using this method. The file will be created
        /// in your root directory.
        /// </remarks>
        /// <param name="bucket">Bucket Id</param>
        /// <param name="file">File to be uploaded</param>
        /// <response code="200">Upload sent</response>
        [HttpPost("{bucket}")]
        [DisableFormValueModelBinding]
        //[RequestSizeLimit(5368709120)]
        public async Task<IActionResult> Post(string bucket)
        {
            if (!IsMultipartContentType(Request.ContentType))
                return BadRequest();

            var ids = new List<Guid>();

            Log.Information($"POSTing files...");

            var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(Request.ContentType).Boundary);
            var reader = new MultipartReader(boundary.Value, Request.Body);

            MultipartSection section;

            IDictionary<string, object> metadata = new Dictionary<string, object>();
            bool isFileLoaded = false;
            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var contentDisposition = section.GetContentDispositionHeader();
                if (contentDisposition.IsFormDisposition())
                {
                    if (isFileLoaded) //clear metada accumulator if file loaded, and start new metadata collection
                    {
                        metadata.Clear();
                        isFileLoaded = false;
                    }

                    var formDataSection = section.AsFormDataSection();

                    string key = formDataSection.Name;
                    string value =  await formDataSection.GetValueAsync();
                    if (!string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
                        metadata.Add(key, value);
                }

                if (contentDisposition.IsFileDisposition())
                {
                    var fileSection = section.AsFileSection();

                    Log.Information($"Saving file {fileSection.FileName}");

                    var id = await _blobStorage.AddFileAsync(Path.GetFileName(fileSection.FileName), fileSection.FileStream, fileSection.Section.ContentType, bucket, metadata);

                    ids.Add(id);

                    var blobInfo = await _blobStorage.GetFileInfo(id, bucket);

                    await _bus.Publish<BlobLoaded>(new
                    {
                        BlobInfo = new LoadedBlobInfo(id, fileSection.FileName, blobInfo.Length, UserID, blobInfo.UploadDateTime, blobInfo.MD5, bucket, metadata),
                        TimeStamp = DateTimeOffset.UtcNow
                    });

                    isFileLoaded = true;
                }
            }

            return Ok(ids);
        }

        /// <summary>
        /// Delete blob
        /// </summary>
        /// <remarks>
        /// Using this endpoint, you can delete the blob associated with a file. Note that
        /// the file itself is not deleted (this can be done on the OSDR website).
        /// </remarks>
        /// <param name="bucket">Bucket Id</param>
        /// <param name="id">Id of file to be deleted</param>
        /// <response code="204">Success - file deleted</response>
        /// <response code="404">File not found or request parameters incorrect</response>
        [HttpDelete("{bucket}/{id}")]
        public async Task<IActionResult> Delete(Guid id, string bucket)
        {
            Log.Information($"Deleting file '{id}' in bucket '{bucket}'");

            var blobInfo = await _blobStorage.GetFileInfo(id, bucket);
            if (blobInfo != null)
            {
                await _blobStorage.DeleteFileAsync(id, bucket);

                Log.Information($"File '{id}' deleted.");

                return Ok();
            }
            else
            {
                Log.Warning($"File '{id}' not found.");

                return NotFound();
            }
        }

        /// <summary>
        /// Get a additional information associated with given blob. 
        /// </summary>
        /// <param name="bucket">Bucket name</param>
        /// <param name="id">Blob Id</param>
        /// <returns></returns>
        /// <response code="200">Success - info sent</response>
        /// <response code="404">File not found or request parameters incorrect</response>
        [HttpGet("{bucket}/{id:guid}/info")]
        public async Task<IActionResult> GetBlobInfo(string bucket, Guid id)
        {
            var blobInfo = await _blobStorage.GetFileInfo(id, bucket);
            return (blobInfo != null) ? Ok(blobInfo) : NotFound() as IActionResult;
        }

        private static bool IsMultipartContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
