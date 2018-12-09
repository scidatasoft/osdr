using Sds.Osdr.IntegrationTests;
using Sds.Osdr.WebApi.IntegrationTests.EndPoints;
using Serilog;
using Serilog.Events;
using System.Net.Http;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    [CollectionDefinition("OSDR Test Harness")]
    public class OsdrTestCollection : ICollectionFixture<OsdrWebTestHarness>
    {
    }

    public class OsdrWebTest : OsdrTest
    {
        public OsdrWebTest(OsdrWebTestHarness fixture, ITestOutputHelper output = null) : base(fixture)
        {
            if (output != null)
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo
                    .TestOutput(output, LogEventLevel.Verbose)
                    .CreateLogger()
                    .ForContext<OsdrTest>();
            }
        }
        public OsdrWebClient JohnApi => WebFixture.JohnApi;
        public OsdrWebClient JaneApi => WebFixture.JaneApi;
        public OsdrWebClient UnauthorizedApi => WebFixture.UnauthorizedApi;

        protected OsdrWebTestHarness WebFixture => Harness as OsdrWebTestHarness;

        //public async Task<Guid> TrainModel(string bucket, string fileName, IDictionary<string, object> metadata = null, bool optimize = false)
        //{
        //    var blobId = AddBlob(JohnId.ToString(), fileName, metadata).Result;
            
        //    var response = await JohnApi.MachineLearningTrain(blobId, JohnId.ToString(), JohnId, JohnId, optimize);
        //    var sss = await response.Content.ReadAsStringAsync();
        //    var jsonResponse = JObject.Parse(await response.Content.ReadAsStringAsync());

        //    var FolderId = jsonResponse["modelFolderId"].ToObject<Guid>();
        //    var ModelCorrelationId = jsonResponse["correlationId"].ToObject<Guid>();
            
        //    Harness.WaitWhileModelTrained(FolderId);

        //    return FolderId;
        //}
        
        //protected async Task<Guid> PredictProperties(string bucket, string fileName, IDictionary<string, object> metadata = null)
        //{
        //    var blobId = await AddBlob(JohnId.ToString(), fileName, metadata);

        //    Guid modelBlobId = Guid.NewGuid();

        //    var response = await JohnApi.MachineLearningPredict(JohnId, modelBlobId, blobId, JohnId, bucket, JohnId, "test prediction");
        //    var jsonContent = JToken.Parse(await response.Content.ReadAsStringAsync());

        //    Guid predictionFolderId = jsonContent["modelFolderId"].ToObject<Guid>();
        //    Guid correlationId = jsonContent["correlationId"].ToObject<Guid>();
            
        //    if (!Harness.Published.Select<PropertiesPredictionFinished>(m => m.Context.Message.Id == predictionFolderId).Any())
        //    {
        //        throw new TimeoutException();
        //    }

        //    return predictionFolderId;
        //}
    }
}