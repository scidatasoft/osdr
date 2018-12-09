using MassTransit;
using Sds.ChemicalProperties.Domain.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sds.MassTransit.Extensions;
using Xunit;
using Sds.ChemicalProperties.Domain.Events;
using FluentAssertions;
using Sds.ChemicalProperties.Domain.Models;

namespace Sds.ChemicalProperties.Tests
{
    public partial class PropertiesCalculationTests : IClassFixture<TestFixture>
    {
        private readonly TestFixture fixture;
        private readonly string _bucket = "UnitTests";

        public PropertiesCalculationTests(TestFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task CalculatePropertiesForAspirin()
        {
            try
            {
                await fixture.Harness.Start();

                var id = NewId.NextGuid();
                var correlationId = NewId.NextGuid();
                var blobId = await fixture.BlobStorage.AddFileAsync("Aspirin.mol", Resource.aspirin, "chemical/x-mdl-sdfile", _bucket);

                await fixture.Harness.Bus.Publish<CalculateChemicalProperties>(new
                {
                    Id = id,
                    Bucket = _bucket,
                    BlobId = blobId,
                    CorrelationId = correlationId,
                    UserId = fixture.UserId
                });

                await fixture.Harness.Published.Any<ChemicalPropertiesCalculated>();

                var allEvents = fixture.Harness.Published.ToList();

                var propertiesCalculated = allEvents.Select<ChemicalPropertiesCalculated>().FirstOrDefault();
                propertiesCalculated.Should().NotBeNull();
                propertiesCalculated.Should().BeEquivalentTo(new
                {
                    Id = id,
                    UserId = fixture.UserId,
                    CorrelationId = correlationId,
                    Result = new CalculatedProperties()
                    {
                        Properties = new Sds.Domain.Property[]
                        {
                            new Sds.Domain.Property("InChI", "InChI=1S/C9H8O4/c1-6(10)13-8-5-3-2-4-7(8)9(11)12/h2-5H,1H3,(H,11,12)"),
                            new Sds.Domain.Property("InChIKey", "BSYNRYMUTXBXSQ-UHFFFAOYSA-N"),
                            new Sds.Domain.Property("NonStdInChI", "InChI=1/C9H8O4/c1-6(10)13-8-5-3-2-4-7(8)9(11)12/h2-5H,1H3,(H,11,12)/f/h11H"),
                            new Sds.Domain.Property("NonStdInChIKey", "BSYNRYMUTXBXSQ-WXRBYKJCNA-N"),
                            new Sds.Domain.Property("SMILES", "CC(=O)OC1C=CC=CC=1C(O)=O"),
                            new Sds.Domain.Property("MOLECULAR_FORMULA", "C9 H8 O4"),
                            new Sds.Domain.Property("MOLECULAR_WEIGHT", 180.157425F),
                            new Sds.Domain.Property("MONOISOTOPIC_MASS", 180.042267F),
                            new Sds.Domain.Property("MOST_ABUNDANT_MASS", 180.042267F),
                        },
                        Issues = new Sds.Domain.Issue[] { }
                    }
                },
                options => options.ExcludingMissingMembers()
                );
                propertiesCalculated.Result.Properties.Count().Should().Be(9);
            }
            finally
            {
                await fixture.Harness.Stop();
            }
        }
    }
}
