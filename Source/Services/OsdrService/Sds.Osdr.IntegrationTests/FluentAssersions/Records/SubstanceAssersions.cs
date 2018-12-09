using FluentAssertions;
using FluentAssertions.Collections;
using Sds.Osdr.Chemicals.Domain.Aggregates;
using Sds.Osdr.Generic.Extensions;
using Sds.Osdr.RecordsFile.Domain;
using System.Collections.Generic;
using System.Linq;

namespace Sds.Osdr.IntegrationTests.FluentAssersions
{
    public static class SubstanceAssersionsExtensions
    {
        public static void EntityShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, Record record)
        {
            record.Should().NotBeNull();

            var expected = new Dictionary<string, object>()
            {
                { "_id", record.Id},
                { "Type", record.RecordType.ToString() },
                { "Name", record.Index },
                { "FileId", record.ParentId },
                { "Blob", new Dictionary<string, object>() {
                    { "_id", record.BlobId },
                    { "Bucket", record.Bucket },
                } },
                { "OwnedBy", record.OwnedBy },
                { "CreatedBy", record.CreatedBy },
                { "CreatedDateTime", record.CreatedDateTime.UtcDateTime },
                { "UpdatedBy", record.UpdatedBy },
                { "UpdatedDateTime", record.UpdatedDateTime.UtcDateTime },
                { "Index", record.Index },
                { "Properties", new Dictionary<string, object> {
                    { "Fields", record.Fields.Select(f => new Dictionary<string, object> {
                        { "Name", f.Name },
                        { "Value", f.Value }
                    }) }
                } },
                { "Status",  record.Status.ToString()},
                { "Version", record.Version }
            };

            if (record.Images.Any())
            {
                expected.Add("Images", record.Images.Select(i => new Dictionary<string, object>
                {
                    { "_id", i.Id },
                    { "Bucket", i.Bucket },
                    { "Format", i.Format },
                    { "Height", i.Height },
                    { "Width", i.Height },
                    { "MimeType", i.MimeType },
                    { "Exception", i.Exception }
                }));
            }

            if (record is Substance)
            {
                (expected["Properties"] as IDictionary<string, object>)["ChemicalProperties"] = record.Properties.Select(p => new Dictionary<string, object> {
                    { "Name", p.Name },
                    { "Value", p.Value },
                    { "Error", p.Error }
                });

                if (record.Issues.Any())
                {
                    (expected["Properties"] as IDictionary<string, object>)["Issues"] = record.Issues.Select(p => new Dictionary<string, object> {
                        { "Code", p.Code },
                        { "AuxInfo", p.AuxInfo },
                        { "Message", p.Message },
                        { "Severity", p.Severity.ToString() },
                        { "Title", p.Title }
                    });
                }
            }

            assertions.Subject.ShouldAllBeEquivalentTo(expected);
        }

        public static void NodeShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, Record record)
        {
            record.Should().NotBeNull();

            var expected = new Dictionary<string, object>()
            {
                { "_id", record.Id},
                { "Type", "Record" },
                { "SubType", record.RecordType.ToString() },
                { "Name", record.Index },
                { "Blob", new Dictionary<string, object>() {
                    { "_id", record.BlobId },
                    { "Bucket", record.Bucket },
                } },
                { "OwnedBy", record.OwnedBy },
                { "CreatedBy", record.CreatedBy },
                { "CreatedDateTime", record.CreatedDateTime.UtcDateTime },
                { "UpdatedBy", record.UpdatedBy },
                { "UpdatedDateTime", record.UpdatedDateTime.UtcDateTime },
                { "Index", record.Index },
                { "ParentId", record.ParentId },
                { "Status",  record.Status.ToString()},
                { "Version", record.Version }
            };

            if (record.Images.Any())
            {
                expected.Add("Images", record.Images.Select(i => new Dictionary<string, object>
                {
                    { "_id", i.Id },
                    { "Bucket", i.Bucket },
                    { "Height", i.Height },
                    { "Width", i.Height },
                    { "MimeType", i.MimeType },
                    { "Scale", i.GetScale() }
                }));
            }

            assertions.Subject.ShouldAllBeEquivalentTo(expected);
        }

        public static void EntityShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, InvalidRecord record)
        {
            record.Should().NotBeNull();

            var expected = new Dictionary<string, object>()
            {
                { "_id", record.Id},
                { "Type", record.RecordType.ToString() },
                { "FileId", record.ParentId },
                { "OwnedBy", record.OwnedBy },
                { "CreatedBy", record.CreatedBy },
                { "CreatedDateTime", record.CreatedDateTime.UtcDateTime },
                { "UpdatedBy", record.UpdatedBy },
                { "UpdatedDateTime", record.UpdatedDateTime.UtcDateTime },
                { "Index", record.Index },
                { "Status",  record.Status.ToString()},
                { "Message",  record.Error},
                { "Version", record.Version }
            };

            assertions.Subject.ShouldAllBeEquivalentTo(expected);
        }

        public static void NodeShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, InvalidRecord record)
        {
            record.Should().NotBeNull();

            var expected = new Dictionary<string, object>()
            {
                { "_id", record.Id},
                { "Type", "Record" },
                { "SubType", record.RecordType.ToString() },
                { "Name", record.Index },
                { "OwnedBy", record.OwnedBy },
                { "CreatedBy", record.CreatedBy },
                { "CreatedDateTime", record.CreatedDateTime.UtcDateTime },
                { "UpdatedBy", record.UpdatedBy },
                { "UpdatedDateTime", record.UpdatedDateTime.UtcDateTime },
                { "ParentId", record.ParentId },
                { "Index", record.Index },
                { "Status",  record.Status.ToString()},
                { "Message",  record.Error},
                { "Version", record.Version }
            };

            assertions.Subject.ShouldAllBeEquivalentTo(expected);
        }
    }
}
