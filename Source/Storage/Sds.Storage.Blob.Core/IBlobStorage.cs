using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Sds.Storage.Blob.Core
{
    public interface IBlobStorage
    {
        /// <summary>
        /// Add file into storage
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <param name="source">Source stream</param>
        /// <param name="contentType">MIME type</param>
        /// <param name="bucketName">Bucket name where file stored</param>
        /// <param name="metadata">Metadata assigned to saved file</param>
        /// <returns>GUID assigned to file in storage</returns>
        Task<Guid> AddFileAsync(string fileName, Stream source, string contentType = "application/octet-stream", string bucketName = null, IDictionary<string, object> metadata = null);

        /// <summary>
        /// Add file into storage
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <param name="source">Source bytes array</param>
        /// <param name="contentType">MIME type</param>
        /// <param name="bucketName">Bucket name where file stored</param>
        /// <param name="metadata">Metadata assigned to saved file</param>
        /// <returns>GUID assigned to file in storage</returns>
        Task<Guid> AddFileAsync(string fileName, byte[] source, string contentType = "application/octet-stream", string bucketName = null, IDictionary<string, object> metadata = null);

        /// <summary>
        /// Add file into storage
        /// </summary>
        /// <param name="id">File Id</param>
        /// <param name="fileName">File name</param>
        /// <param name="source">Source bytes array</param>
        /// <param name="contentType">MIME type</param>
        /// <param name="bucketName">Bucket name where file stored</param>
        /// <param name="metadata">Metadata assigned to saved file</param>
        /// <returns>GUID assigned to file in storage</returns>
        Task AddFileAsync(Guid id, string fileName, byte[] source, string contentType = "application/octet-stream", string bucketName = null, IDictionary<string, object> metadata = null);

        /// <summary>
        /// Add file into storage with given id
        /// </summary>
        /// <param name="id">File Id</param>
        /// <param name="fileName">File name</param>
        /// <param name="source">Source stream</param>
        /// <param name="contentType">MIME type</param>
        /// <param name="bucketName">Bucket name where file stored</param>
        /// <param name="metadata">Metadata assigned to saved file</param>
        /// <returns></returns>
        Task AddFileAsync(Guid id, string fileName, Stream source, string contentType = "application/octet-stream", string bucketName = null, IDictionary<string, object> metadata = null);

        /// <summary>
        /// Delete file from storage
        /// </summary>
        /// <param name="id">File Id</param>
        /// <param name="bucketName">Bucket name where file stored</param>
        /// <returns></returns>
        Task DeleteFileAsync(Guid id, string bucketName = null);

        /// <summary>
        /// Get file from storage
        /// </summary>
        /// <param name="id">File Id</param>
        /// <param name="bucketName">Bucket name where file stored</param>
        /// <returns>Created blob object with file data</returns>
        Task<IBlob> GetFileAsync(Guid id, string bucketName = null);

        /// <summary>
        /// Get file from storage
        /// </summary>
        /// <param name="id">File Id</param>
        /// <param name="destination">The destination</param>
        /// <param name="bucketName">Bucket name where file stored</param>
        Task DownloadFileToStreamAsync(Guid id, Stream destination, string bucketName = null);

        /// <summary>
        /// Get file info if exist or null
        /// </summary>
        /// <param name="id">File Id</param>
        /// <param name="bucketName">Bucket name where file stored</param>
        /// <returns></returns>
        Task<IBlobInfo> GetFileInfo(Guid id, string bucketName = null);

        /// <summary>
        /// Opens a Stream that can be used by the application to write data to a BlobStorage file.
        /// </summary>
        /// <param name="id">File Id</param>
        /// <param name="fileName">File name</param>
        /// <param name="contentType">MIME type</param>
        /// <param name="bucketName">Bucket name where file stored</param>
        /// <param name="metadata">Metadata assigned to saved file</param>
        /// <returns></returns>
        Stream OpenUploadStream(Guid id, string fileName, string contentType = "application/octet-stream", string bucketName = null, IDictionary<string, object> metadata = null);

        /// <summary>
        /// Opens a Stream that can be used by the application to write data to a BlobStorage file.
        /// </summary>
        /// <param name="id">File Id</param>
        /// <param name="fileName">File name</param>
        /// <param name="contentType">MIME type</param>
        /// <param name="bucketName">Bucket name where file stored</param>
        /// <param name="metadata">Metadata assigned to saved file</param>
        /// <returns></returns>
        Task<Stream> OpenUploadStreamAsync(Guid id, string fileName, string contentType = "application/octet-stream", string bucketName = null, IDictionary<string, object> metadata = null);
    }
}