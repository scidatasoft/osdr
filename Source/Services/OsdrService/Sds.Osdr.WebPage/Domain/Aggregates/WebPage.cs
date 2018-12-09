using Sds.Osdr.Generic.Domain;
using Sds.Osdr.RecordsFile.Domain.Events.Files;
using Sds.Osdr.WebPage.Domain.Events;
using System;


namespace Sds.Osdr.WebPage.Domain
{
    public class WebPage: File
    {
        public long TotalRecords { get; private set; }
        /// <summary>
        /// Url of webpage
        /// </summary>
        public string Url { get; private set; }

        /// <summary>
        /// Blob Id of Pdf in Bucket
        /// </summary>
        public Guid JsonBlobId { get; private set; }

        private void Apply(WebPageCreated e)
        {
            Url = e.Url;
            UpdatedBy = e.UserId;
        }

        private void Apply(WebPageUpdated e)
        {
            JsonBlobId = e.JsonBlobId;
            UpdatedBy = e.UserId;
        }

        protected WebPage()
        {
        }

        private void Apply(TotalRecordsUpdated e)
        {
            TotalRecords = e.TotalRecords;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        public void UpdateTotalRecords(Guid userId, long total)
        {
            ApplyChange(new TotalRecordsUpdated(Id, userId, total));
        }

        public void UpdateWebPage(Guid userId, string bucket, Guid blobId)
        {
            ApplyChange(new WebPageUpdated(Id, userId, bucket, blobId));
        }

        public WebPage(Guid id, Guid userId, Guid? parentId, string fileName, FileStatus fileStatus, string bucket, Guid blobId, long length, string md5, string url)
            : base(id, userId, parentId, fileName, fileStatus, bucket, blobId, length, md5, FileType.WebPage)
        {
            Id = id;
            ApplyChange(new WebPageCreated(Id, userId, url));
        }
    }
}
