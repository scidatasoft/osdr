using FluentAssertions;
using FluentAssertions.Collections;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Extensions;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.Office.Domain;
using Sds.Osdr.Pdf.Domain;
using Sds.Osdr.Tabular.Domain;
using System.Collections.Generic;
using System.Linq;

namespace Sds.Osdr.IntegrationTests.FluentAssersions
{
    public static class FileAssersionsExtensions
    {
        public static void EntityShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, File file)
        {
            assertions.Subject.ShouldAllBeEquivalentTo(new Dictionary<string, object>()
            {
                { "_id", file.Id},
                { "Blob", new Dictionary<string, object>() {
                    { "_id", file.BlobId },
                    { "Bucket", file.Bucket },
                    { "Length", file.Length },
                    { "Md5", file.Md5 }
                } },
                { "SubType", file.Type.ToString() },
                { "OwnedBy", file.OwnedBy },
                { "CreatedBy", file.CreatedBy },
                { "CreatedDateTime", file.CreatedDateTime.UtcDateTime },
                { "UpdatedBy", file.UpdatedBy },
                { "UpdatedDateTime", file.UpdatedDateTime.UtcDateTime },
                { "Name", file.FileName },
                { "Status",  file.Status.ToString()},
                { "ParentId", file.ParentId },
                { "Images", file.Images.Select(i => new Dictionary<string, object> {
                    { "_id", i.Id },
                    { "Bucket", i.Bucket },
                    { "Height", i.Height },
                    { "Width", i.Height },
                    { "MimeType", i.MimeType },
                    { "Scale", i.GetScale() }
                })},
                { "Version", file.Version }
            });
        }

        public static void NodeShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, File file)
        {
            assertions.Subject.ShouldAllBeEquivalentTo(new Dictionary<string, object>()
            {
                { "_id", file.Id},
                { "Type", "File" },
                { "SubType", file.Type.ToString() },
                { "Blob", new Dictionary<string, object>() {
                    { "_id", file.BlobId },
                    { "Bucket", file.Bucket },
                    { "Length", file.Length },
                    { "Md5", file.Md5 }
                } },
                { "OwnedBy", file.OwnedBy },
                { "CreatedBy", file.CreatedBy },
                { "CreatedDateTime", file.CreatedDateTime.UtcDateTime },
                { "UpdatedBy", file.UpdatedBy },
                { "UpdatedDateTime", file.UpdatedDateTime.UtcDateTime },
                { "Name", file.FileName },
                { "Status",  file.Status.ToString()},
                { "ParentId", file.ParentId },
                { "Images", file.Images.Select(i => new Dictionary<string, object> {
                    { "_id", i.Id },
                    { "Bucket", file.Bucket },
                    { "Height", i.Height },
                    { "Width", i.Height },
                    { "MimeType", i.MimeType },
                    { "Scale", i.GetScale() }
                })},
                { "Version", file.Version }
            });
        }

        public static void OfficeNodeShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, OfficeFile file)
        {
            assertions.Subject.ShouldAllBeEquivalentTo(new Dictionary<string, object>()
            {
                { "_id", file.Id},
                { "Type", "File" },
                { "SubType", file.Type.ToString() },
                { "Blob", new Dictionary<string, object>() {
                    { "_id", file.BlobId },
                    { "Bucket", file.Bucket },
                    { "Length", file.Length },
                    { "Md5", file.Md5 }
                } },
                { "Status",  file.Status.ToString()},
                { "OwnedBy", file.OwnedBy },
                { "CreatedBy", file.CreatedBy },
                { "CreatedDateTime", file.CreatedDateTime.UtcDateTime },
                { "UpdatedBy", file.UpdatedBy },
                { "UpdatedDateTime", file.UpdatedDateTime.UtcDateTime },
                { "Name", file.FileName },

                { "ParentId", file.ParentId },
                { "Version", file.Version },
                { "Pdf", new Dictionary<string, object>() {
                    { "BlobId", file.PdfBlobId },
                    { "Bucket", file.PdfBucket }
                } },
                { "Images", file.Images.Select(i => new Dictionary<string, object> {
                    { "_id", i.Id },
                    { "Bucket", i.Bucket },
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

        public static void FileViewShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, OfficeFile file)
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
                    { "Height", i.Height },
                    { "Width", i.Height },
                    { "MimeType", i.MimeType },
                    { "Scale", i.GetScale() }
                })},
                { "Properties", new Dictionary<string, object>(){
                    { "Metadata",  new Dictionary<string, object>(){
                        {"CreatedBy", file.Metadata.Where(p=> p.Name == "CreatedBy" ).Single()},
                        { "CreatedDateTime", file.Metadata.Where(p=> p.Name == "CreatedDateTime" ).Single()  }
                    }
                }}
            }});
        }

        public static void WebPageEntityShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, WebPage.Domain.WebPage file)
        {
            var expected = new Dictionary<string, object>()
            {
                { "_id", file.Id},
                { "Blob", new Dictionary<string, object>() {
                    { "_id", file.BlobId },
                    { "Bucket", file.Bucket },
                    { "Length", file.Length },
                    { "Md5", file.Md5 }
                } },
                { "SubType", file.Type.ToString() },
                { "OwnedBy", file.OwnedBy },
                { "CreatedBy", file.CreatedBy },
                { "CreatedDateTime", file.CreatedDateTime.UtcDateTime },
                { "UpdatedBy", file.UpdatedBy },
                { "UpdatedDateTime", file.UpdatedDateTime.UtcDateTime },
                { "Name", file.FileName },
                { "Status",  file.Status.ToString()},
                { "ParentId", file.ParentId },
                { "Url", file.Url },
                { "Version", file.Version },
                { "Json", new Dictionary<string, object>() {
                    { "BlobId", file.JsonBlobId },
                    { "Bucket", file.Bucket },
                } }
            };

            if(file.TotalRecords > 0)
            {
                expected.Add("TotalRecords", file.TotalRecords);
            }

            if (file.Images.Any())
            {
                expected.Add("Images", file.Images.Select(i => new Dictionary<string, object>
                {
                    { "_id", i.Id },
                    { "Height", i.Height },
                    { "Width", i.Height },
                    { "MimeType", i.MimeType },
                    { "Scale", i.GetScale() }
                }));
            }

            assertions.Subject.ShouldAllBeEquivalentTo(expected);

        }

        public static void WebNodeShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, WebPage.Domain.WebPage file)
        {
            var expected = new Dictionary<string, object>()
            {
                { "_id", file.Id},
                { "Type", "File" },
                { "SubType", file.Type.ToString() },
                { "Blob", new Dictionary<string, object>() {
                    { "_id", file.JsonBlobId },
                    { "Bucket", file.Bucket },
                    { "Length", file.Length },
                    { "Md5", file.Md5 }
                } },
                { "Status",  file.Status.ToString()},
                { "OwnedBy", file.OwnedBy },
                { "CreatedBy", file.CreatedBy },
                { "CreatedDateTime", file.CreatedDateTime.UtcDateTime },
                { "UpdatedBy", file.UpdatedBy },
                { "UpdatedDateTime", file.UpdatedDateTime.UtcDateTime },
                { "Name", file.FileName },
                { "ParentId", file.ParentId },
                { "Version", file.Version }
            };

            if (file.TotalRecords > 0)
            {
                expected.Add("TotalRecords", file.TotalRecords);
            }

            if (file.Images.Any())
            {
                expected.Add("Images", file.Images.Select(i => new Dictionary<string, object>
                {
                    { "_id", i.Id },
                    { "Height", i.Height },
                    { "Width", i.Height },
                    { "MimeType", i.MimeType },
                    { "Scale", i.GetScale() }
                }));
            }

            assertions.Subject.ShouldAllBeEquivalentTo(expected);
        }
        
        public static void PdfEntityShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, PdfFile file)
        {
            var expected = new Dictionary<string, object>()
            {
                { "_id", file.Id},
                { "Blob", new Dictionary<string, object>() {
                    { "_id", file.BlobId},
                    { "Bucket", file.Bucket },
                    { "Length", file.Length },
                    { "Md5", file.Md5 }
                } },
                { "SubType", FileType.Pdf.ToString() },
                { "OwnedBy", file.OwnedBy },
                { "CreatedBy", file.CreatedBy },
                { "CreatedDateTime", file.CreatedDateTime.UtcDateTime},
                { "UpdatedBy", file.UpdatedBy },
                { "UpdatedDateTime", file.UpdatedDateTime.UtcDateTime},
                { "ParentId", file.ParentId },
                { "Name", file.FileName },
                { "Status", file.Status.ToString() },
                { "Version", file.Version }
            };
            
            if (file.Images.Any())
            {
                expected.Add("Images", file.Images.Select(i => new Dictionary<string, object>
                {
                    { "_id", i.Id },
                    { "Height", i.Height },
                    { "Width", i.Height },
                    { "MimeType", i.MimeType },
                    { "Scale", i.GetScale() },
                    { "Bucket", i.Bucket }
                }));
            }

            assertions.Subject.ShouldAllBeEquivalentTo(expected);
        }

        public static void TabularEntityShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, TabularFile file)
        {
            var expected = new Dictionary<string, object>()
            {
                { "_id", file.Id},
                { "Blob", new Dictionary<string, object>() {
                    { "_id", file.BlobId},
                    { "Bucket", file.Bucket },
                    { "Length", file.Length },
                    { "Md5", file.Md5 }
                } },
                { "SubType", FileType.Tabular.ToString() },
                { "OwnedBy", file.OwnedBy },
                { "CreatedBy", file.CreatedBy },
                { "CreatedDateTime", file.CreatedDateTime.UtcDateTime},
                { "UpdatedBy", file.UpdatedBy },
                { "UpdatedDateTime", file.UpdatedDateTime.UtcDateTime},
                { "ParentId", file.ParentId },
                { "Name", file.FileName },
                { "Status", file.Status.ToString() },
                { "Version", file.Version }
            };
            
            if (file.Images.Any())
            {
                expected.Add("Images", file.Images.Select(i => new Dictionary<string, object>
                {
                    { "_id", i.Id },
                    { "Height", i.Height },
                    { "Width", i.Height },
                    { "MimeType", i.MimeType },
                    { "Scale", i.GetScale() },
                    { "Bucket", i.Bucket }
                }));
            }

            assertions.Subject.ShouldAllBeEquivalentTo(expected);
        }
        
        public static void ModelNodeShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, Model model)
        {
            var expected = new Dictionary<string, object>()
            {
                { "_id", model.Id},
                { "Type", "Model" },
                { "Blob", new Dictionary<string, object>() {
                    { "_id", model.BlobId },
                    { "Bucket", model.Bucket }
                } },
                { "Status",  model.Status.ToString()},
                { "OwnedBy", model.OwnedBy },
                { "CreatedBy", model.CreatedBy },
                { "CreatedDateTime", model.CreatedDateTime.UtcDateTime },
                { "UpdatedBy", model.UpdatedBy },
                { "UpdatedDateTime", model.UpdatedDateTime.UtcDateTime },
                { "Name", model.Name },
                { "ParentId", model.ParentId },
                { "Version", model.Version }
            };

            if (model.Images.Any())
            {
                expected.Add("Images", model.Images.Select(i => new Dictionary<string, object>
                {
                    { "_id", i.Id },
                    { "Height", i.Height },
                    { "Width", i.Height },
                    { "MimeType", i.MimeType },
                    { "Scale", i.GetScale() },
                    { "Bucket", i.Bucket }
                }));
            }
            assertions.Subject.ShouldAllBeEquivalentTo(expected);
        }

        public struct Fingerprint
        {
            public int radius { get; set; }
            public int size { get; set; }
            public int type { get; set; }
        }

        public static void ModelEntityShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, Model model)
        {
            var expected = new Dictionary<string, object>
            {
                { "_id", model.Id},
                { "OwnedBy", model.OwnedBy },
                { "CreatedBy", model.CreatedBy },
                { "CreatedDateTime", model.CreatedDateTime.UtcDateTime},
                { "KFold", model.KFold },
                { "TestDatasetSize", model.TestDatasetSize },
                { "SubSampleSize", model.SubSampleSize },
                { "ClassName", model.ClassName },
                { "Fingerprints",  model.Fingerprints.Select(f => new Dictionary<string, object>
                {
                    { "Type", f.Type },
                    { "Size", f.Size },
                    { "Radius", f.Radius }
                }) },
                { "UpdatedBy", model.UpdatedBy },
                { "UpdatedDateTime", model.UpdatedDateTime.UtcDateTime},
                { "ParentId", model.ParentId },
                { "Status", model.Status.ToString() },
                { "Method", model.Method.ToString() },
                { "Scaler", model.Scaler },
                { "Version", model.Version },
                { "Blob", new Dictionary<string, object>() {
                    { "_id", model.BlobId},
                    { "Bucket", model.Bucket }
                } },
                { "Name", model.Name },
                { "DisplayMethodName", model.DisplayMethodName }
            };

            if (model.Modi != 0)
            {
                expected.Add("Modi", model.Modi);
            }

            if (model.Dataset != null)
            {
                expected.Add("Dataset", new Dictionary<string, object>() {
                    { "BlobId", model.Dataset.BlobId},
                    { "Bucket", model.Dataset.Bucket },
                    { "Title", model.Dataset.Title },
                    { "Description", model.Dataset.Description }
                });
            }

            if (model.Property != null)
            {
                expected.Add("Property", new Dictionary<string, object>() {
                    { "Category", model.Property.Category },
                    { "Description", model.Property.Description },
                    { "Name", model.Property.Name },
                    { "Units", model.Property.Units }
                });
            }

            if (model.Images.Any())
            {
                expected.Add("Images", model.Images.Select(i => new Dictionary<string, object>
                {
                    { "_id", i.Id },
                    { "Height", i.Height },
                    { "Width", i.Height },
                    { "MimeType", i.MimeType },
                    { "Scale", i.GetScale() },
                    { "Bucket", i.Bucket }
                }));
            }

            if (model.Metadata != null)
            {
                var jsonMeta = JsonConvert.SerializeObject(model.Metadata);
                var modelMetaBson = BsonSerializer.Deserialize<Dictionary<string, object>>(jsonMeta);

                expected.Add("Metadata", modelMetaBson);
            }
            assertions.Subject.ShouldAllBeEquivalentTo(expected);
        }
    }
}
