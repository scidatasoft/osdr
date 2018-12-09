using MongoDB.Bson;
using MongoDB.Driver;
using Sds.ChemicalExport.Domain;
using Sds.ChemicalExport.Domain.Models;
using Sds.Storage.Blob.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.ChemicalExport.FileRecordsProvider
{
    public class OsdrRecordsProvider : IRecordsProvider
    {
        private readonly IMongoDatabase database;
        private readonly IBlobStorage blobStorage;

        protected IMongoCollection<BsonDocument> Files { get { return database.GetCollection<BsonDocument>("Files"); } }
        protected IMongoCollection<BsonDocument> Records { get { return database.GetCollection<BsonDocument>("Records"); } }


        public OsdrRecordsProvider(IMongoDatabase database, IBlobStorage blobStorage)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            this.blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
        }
    
        public Task<IEnumerator<Record>> GetRecords(Guid blobId, string bucket
            //, int start, int count
            )
        {
            var fileFilter = Builders<BsonDocument>.Filter.Where(d => d["Blob._id"] == blobId && d["Blob.Bucket"] == bucket);

            var fileView = Files.Find(fileFilter).First();

            var fileId = (Guid)fileView["_id"];

            var records = Records.Find(Builders<BsonDocument>.Filter.Where(d => d["FileId"] == fileId))
                .Project(d =>
                    new Record
                    {
                        Index = d["Index"].ToInt32(),
                        Mol = GetMolData(blobStorage, fileId, bucket, d["Index"].ToInt32()),
                        Properties = GetAllProperties(fileId, bucket, d["Index"].ToInt32())
                    }).ToEnumerable().OrderBy(r=> r.Index);
          
            return Task.FromResult(records.GetEnumerator());
        }

        private string GetMolData(IBlobStorage storage, Guid fileId, string bucket, long index)
        {
            var filter = Builders<BsonDocument>.Filter.Where(d => d["FileId"] == fileId && d["Index"] == index);

            var recordView = Records.Find(filter).First();

            var molBlobStream = blobStorage.GetFileAsync(recordView["Blob"]["_id"].AsGuid, bucket).Result.GetContentAsStream();

            using (StreamReader reader = new StreamReader(molBlobStream))
            {
                return reader.ReadToEnd();
            }
        }

        private IEnumerable<PropertyValue> GetAllProperties(Guid fileId, string bucket, long index)
        {
            var filter = Builders<BsonDocument>.Filter.Where(d => d["FileId"] == fileId && d["Index"] == index);
            var recordView = Records.Find(filter).First();

            var chemicalProperties = recordView["Properties"]["ChemicalProperties"]
                .AsBsonArray.Select(prop => 
                new PropertyValue
                {
                    Name = "Properties.ChemicalProperties."+prop["Name"].ToString(),
                    Value = prop["Value"].ToString()
                });

            var fields = recordView["Properties"]["Fields"]
                .AsBsonArray.Select(prop =>
                new PropertyValue
                {
                    Name = "Properties.Fields." + prop["Name"].ToString(),
                    Value = prop["Value"].ToString()
                });

            //var issues = recordView["Properties"]["Issues"]
            //    .AsBsonArray.Select(prop =>
            //    new PropertyValue
            //    {
            //        Name = "Properties.Issues." + prop["Name"].ToString(),
            //        Value = prop["Value"].ToString()
            //    });

            return new List<IEnumerable<PropertyValue>> { chemicalProperties, fields }.SelectMany(x => x);

        }
    }
}
