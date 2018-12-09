using Sds.ChemicalExport.Domain;
using Sds.ChemicalExport.Domain.Models;
using Sds.SdfParser;
using Sds.Storage.Blob.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.ChemicalExport.FileRecordsProvider
{
    public class BlobStorageRecordsProvider : IRecordsProvider
    {
        private IBlobStorage blobStorage;


        public BlobStorageRecordsProvider(IBlobStorage blobStorage)
        {
            this.blobStorage = blobStorage;
        }

        public async Task<IEnumerator<Record>> GetRecords(Guid fileId, string bucket)
        {
            var blob = await blobStorage.GetFileAsync(fileId, bucket);

            if (blob == null)
            {
                throw new FileNotFoundException($"Blob with Id {fileId} not found.");
            }

            IEnumerable<FileParser.Record> records;

            switch (Path.GetExtension(blob.Info.FileName).ToLower())
            {
                case ".mol":
                case ".sdf":
                    records = new SdfIndigoParser(blob.GetContentAsStream());
                    break;
                case ".cdx":
                    records = new CdxParser.CdxParser(blob.GetContentAsStream());
                    break;
                default:
                    records = null;
                    break;
            }
            return records.Select(r=> new Record {
                Mol = r.Data,
                Properties = r.Properties.Select(p => new PropertyValue
                {
                    Name = "Properties.Fields." + p.Name,
                    Value = p.Value
                }),
                Index = r.Index
            }).GetEnumerator();
           
        }
    }
}

