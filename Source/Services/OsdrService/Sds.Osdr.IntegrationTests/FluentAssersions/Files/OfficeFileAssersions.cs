using FluentAssertions;
using FluentAssertions.Collections;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Extensions;
using Sds.Osdr.Office.Domain;
using System.Collections.Generic;
using System.Linq;

namespace Sds.Osdr.IntegrationTests.FluentAssersions
{
    public static class OfficeFileAssersionsExtensions
    {
        public static void EntityShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, OfficeFile file)
        {
            assertions.Subject.ShouldAllBeEquivalentTo(new Dictionary<string, object>
            {
                { "_id", file.Id},
                { "Blob", new Dictionary<string, object>() {
                    { "_id", file.BlobId},
                    { "Bucket", file.Bucket },
                    { "Length", file.Length },
                    { "Md5", file.Md5 }
                } },
                { "SubType", FileType.Office.ToString() },
                { "OwnedBy", file.OwnedBy },
                { "CreatedBy", file.CreatedBy },
                { "CreatedDateTime", file.CreatedDateTime.UtcDateTime},
                { "UpdatedBy", file.UpdatedBy },
                { "UpdatedDateTime", file.UpdatedDateTime.UtcDateTime},
                { "ParentId", file.ParentId },
                { "Name", file.FileName },
                { "Status", file.Status.ToString() },
                { "Version", file.Version },
                { "Pdf", new Dictionary<string, object>() {
                    { "BlobId", file.PdfBlobId },
                    { "Bucket", file.PdfBucket }
                } },
                { "Images", file.Images.Select(i => new Dictionary<string, object> {
                    { "_id", i.Id },
                    { "Bucket", file.Bucket },
                    { "Height", i.Height },
                    { "Width", i.Height },
                    { "MimeType", i.MimeType },
                    { "Scale", i.GetScale() }
                })},

                { "Properties", new Dictionary<string, object>(){
                    { "Metadata",  file.Metadata.Select(p => new Dictionary<string, object>{
                        { "Name", p.Name },
                        { "Value", p.Value },
                        { "Error", p.Error }
                    } ) }
                }
            }});
        }
    }
}
