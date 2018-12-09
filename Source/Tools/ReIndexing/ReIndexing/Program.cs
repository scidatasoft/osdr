using Nest;
using MongoDB.Driver;
using System;
using System.Configuration;
using Sds.Storage.Blob.GridFs;
using CommandLine;

namespace ReIndexing
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed(opt =>
            {
                string connectionString = ConfigurationManager.AppSettings["mongodb:connection"].ToConnectionString();
                var db = new MongoClient(connectionString).GetDatabase(ConfigurationManager.AppSettings["mongodb:database-name"]);
                var blobStorage = new GridFsStorage(connectionString, ConfigurationManager.AppSettings["mongodb:database-name"]);

                string elasticConnectionString = ConfigurationManager.AppSettings["ElasticSearch:ConnectionString"].ToConnectionString();
                var node = new Uri(elasticConnectionString);
                var client = new ElasticClient(node);
                
                //Set document parsing pipeline for elasticsearch
                client.PutPipeline("process_blob", p => p
                   .Description("Document attachment pipeline")
                   .Processors(pr => pr
                       .Attachment<object>(a => a.Field("Blob.Base64Content").TargetField("Blob.ParsedContent").IgnoreMissing())
                       .Remove<object>(r => r.Field("Blob.Base64Content"))
                       ));

                var reindexer = new OsdrReindexer(db, client, blobStorage);
                Console.WriteLine($"Using MongoDB: {connectionString}");
                Console.WriteLine($"Using Elasticsearch: {elasticConnectionString}");
                reindexer.Reindex(opt.Entities);
            });
        }
    }
}
