using FluentAssertions;
using Newtonsoft.Json.Linq;
using Sds.Osdr.BddTests.FluentAssersions;
using Sds.Osdr.BddTests.Traits;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.WebApi.IntegrationTests.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.WebApi.IntegrationTests
{
    [Collection("OSDR Test Harness")]
    public class PredictPropertiesValidCase : OsdrWebTest
    {
        private Guid BlobId { get { return GetBlobId(FolderId); } }
        private Guid FolderId { get; set; }

        public PredictPropertiesValidCase(OsdrWebTestHarness fixture, ITestOutputHelper output) 
            : base(fixture, output)
        {
            FolderId = PredictProperties(JohnId.ToString(), "combined lysomotrophic.sdf", new Dictionary<string, object>() { { "parentId", JohnId } }).Result;
        }
        
        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlPrediction_ThereAreNoErrors()
        {
            Fixture.GetFaults().Should().BeEmpty();

            await Task.CompletedTask;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlPrediction_SingleCsvFileProcessed()
        {
            var tabulars = await Fixture.GetDependentFiles(FolderId, FileType.Tabular);
            tabulars.Should().NotBeNullOrEmpty();
            tabulars.Should().HaveCount(1);

            foreach (var tabularId in tabulars)
            {
                var tabularResponse = await JohnApi.GetFileEntityById(tabularId);
                var tabularJson = JToken.Parse(await tabularResponse.Content.ReadAsStringAsync());
                
                tabularJson.Should().ContainsJson($@"
                {{
                    'id': '{tabularId}',
                    'blob': *EXIST*,
                    'subType': 'Tabular',
                    'ownedBy': '{JohnId}',
                    'createdBy': '{JohnId}',
                    'createdDateTime': *EXIST*,
                    'updatedBy': '{JohnId}',
                    'updatedDateTime': *EXIST*,
                    'parentId': '{FolderId}',
                    'name': 'PropertiesPrediction.csv',
                    'status': 'Processed',
                    'version': 7
                }}");
            }

            await Task.CompletedTask;
        }

        [Fact, WebApiTrait(TraitGroup.All, TraitGroup.MachineLearning)]
        public async Task MlPrediction_PredictionCsvFilePersisted()
        {
            var tabulars = await Fixture.GetDependentFiles(FolderId, FileType.Tabular);
            tabulars.Should().NotBeNullOrEmpty();
            tabulars.Should().HaveCount(1);

            foreach (var tabularId in tabulars)
            {
                var tabularResponse = await JohnApi.GetNodeById(tabularId);
                var tabularJson = JToken.Parse(await tabularResponse.Content.ReadAsStringAsync());
                
                tabularJson.Should().ContainsJson($@"
                {{
                    'id': '{tabularId}',
                    'blob': *EXIST*,
                    'subType': 'Tabular',
                    'ownedBy': '{JohnId}',
                    'createdBy': '{JohnId}',
                    'createdDateTime': *EXIST*,
                    'updatedBy': '{JohnId}',
                    'updatedDateTime': *EXIST*,
                    'parentId': '{FolderId}',
                    'name': 'PropertiesPrediction.csv',
                    'status': 'Processed',
                    'version': 7
                }}");
            }

            await Task.CompletedTask;
        }
    }
}