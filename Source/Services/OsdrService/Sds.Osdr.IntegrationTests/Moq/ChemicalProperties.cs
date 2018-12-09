using MassTransit;
using Sds.ChemicalProperties.Domain.Commands;
using Sds.ChemicalProperties.Domain.Events;
using Sds.ChemicalProperties.Domain.Models;
using Sds.Domain;
using Sds.Storage.Blob.Core;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.IntegrationTests.Moq
{
    public class ChemicalProperties : IConsumer<CalculateChemicalProperties>
    {
        private readonly IBlobStorage _blobStorage;

        public ChemicalProperties(IBlobStorage blobStorage)
        {
            _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage)); ;
        }

        public async Task Consume(ConsumeContext<CalculateChemicalProperties> context)
        {
            await context.Publish<ChemicalPropertiesCalculated>(new
            {
                Id = context.Message.Id,
                CorrelationId = context.Message.CorrelationId,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow,
                Result = new CalculatedProperties()
                {
                    Issues = new Issue[] { },
                    Properties = new Property[] {
                        new Property("InChI", "InChI=1S/C98H138N24O33/c1-5-52(4)82(96(153)122-39-15-23-70(122)92(149)114-60(30-34-79(134)135)85(142)111-59(29-33-78(132)133)86(143)116-64(43-55-24-26-56(123)27-25-55)89(146)118-67(97(154)155)40-51(2)3)119-87(144)61(31-35-80(136)137)112-84(141)58(28-32-77(130)131)113-88(145)63(42-54-18-10-7-11-19-54)117-90(147)66(45-81(138)139)110-76(129)50-107-83(140)65(44-71(100)124)109-75(128)49-106-73(126)47-104-72(125)46-105-74(127)48-108-91(148)68-21-13-38-121(68)95(152)62(20-12-36-103-98(101)102)115-93(150)69-22-14-37-120(69)94(151)57(99)41-53-16-8-6-9-17-53/h6-11,16-19,24-27,51-52,57-70,82,123H,5,12-15,20-23,28-50,99H2,1-4H3,(H2,100,124)(H,104,125)(H,105,127)(H,106,126)(H,107,140)(H,108,148)(H,109,128)(H,110,129)(H,111,142)(H,112,141)(H,113,145)(H,114,149)(H,115,150)(H,116,143)(H,117,147)(H,118,146)(H,119,144)(H,130,131)(H,132,133)(H,134,135)(H,136,137)(H,138,139)(H,154,155)(H4,101,102,103)/t52-,57+,58-,59-,60-,61-,62-,63-,64-,65-,66-,67-,68-,69-,70-,82-/m0/s1"),
                        new Property("InChIKey", "OIRCOABEOLEUMC-GEJPAHFPSA-N"),
                        new Property("NonStdInChI", "InChI=1/C98H138N24O33/c1-5-52(4)82(96(153)122-39-15-23-70(122)92(149)114-60(30-34-79(134)135)85(142)111-59(29-33-78(132)133)86(143)116-64(43-55-24-26-56(123)27-25-55)89(146)118-67(97(154)155)40-51(2)3)119-87(144)61(31-35-80(136)137)112-84(141)58(28-32-77(130)131)113-88(145)63(42-54-18-10-7-11-19-54)117-90(147)66(45-81(138)139)110-76(129)50-107-83(140)65(44-71(100)124)109-75(128)49-106-73(126)47-104-72(125)46-105-74(127)48-108-91(148)68-21-13-38-121(68)95(152)62(20-12-36-103-98(101)102)115-93(150)69-22-14-37-120(69)94(151)57(99)41-53-16-8-6-9-17-53/h6-11,16-19,24-27,51-52,57-70,82,123H,5,12-15,20-23,28-50,99H2,1-4H3,(H2,100,124)(H,104,125)(H,105,127)(H,106,126)(H,107,140)(H,108,148)(H,109,128)(H,110,129)(H,111,142)(H,112,141)(H,113,145)(H,114,149)(H,115,150)(H,116,143)(H,117,147)(H,118,146)(H,119,144)(H,130,131)(H,132,133)(H,134,135)(H,136,137)(H,138,139)(H,154,155)(H4,101,102,103)/t52-,57+,58-,59-,60-,61-,62-,63-,64-,65-,66-,67-,68-,69-,70-,82-/m0/s1/f/h101,103-119,130,132,134,136,138,154H,100,102H2/b101-98?"),
                        new Property("NonStdInChIKey", "OIRCOABEOLEUMC-JYQNKMQNNA-N"),
                        new Property("SMILES", "CC(C)C[C@H](NC(=O)[C@H](CC1=CC=C(O)C=C1)NC(=O)[C@H](CCC(O)=O)NC(=O)[C@H](CCC(O)=O)NC(=O)[C@@H]1CCCN1C(=O)[C@@H](NC(=O)[C@H](CCC(O)=O)NC(=O)[C@H](CCC(O)=O)NC(=O)[C@H](CC1=CC=CC=C1)NC(=O)[C@H](CC(O)=O)NC(=O)CNC(=O)[C@H](CC(N)=O)NC(=O)CNC(=O)CNC(=O)CNC(=O)CNC(=O)[C@@H]1CCCN1C(=O)[C@H](CCCNC(N)=N)NC(=O)[C@@H]1CCCN1C(=O)[C@H](N)CC1=CC=CC=C1)[C@@H](C)CC)C(O)=O"),
                        new Property("MOLECULAR_FORMULA", "C98 H138 N24 O33"),
                        new Property("MOLECULAR_WEIGHT", "2180.2854"),
                        new Property("MONOISOTOPIC_MASS", "2178.98584"),
                        new Property("MOST_ABUNDANT_MASS", "2179.98926")
                    }
                }
            });
        }
    }
}
