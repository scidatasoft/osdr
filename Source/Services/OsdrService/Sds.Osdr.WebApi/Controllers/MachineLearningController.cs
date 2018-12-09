using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.Commands;
using Sds.Osdr.WebApi.Extensions;
using Sds.Osdr.WebApi.Models;
using Sds.Osdr.WebApi.Requests;
using Sds.Storage.KeyValue.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sds.Osdr.WebApi.Controllers
{
    [Route("api/[controller]")]
    public class MachineLearningController : Controller
    {
        private IBusControl _bus;
        protected IMongoDatabase _database;
        private IMongoCollection<BsonDocument> _models;
        private IMongoCollection<BsonDocument> _accessPermissions;
        private IKeyValueRepository _keyValueRepository;
        protected SingleStructurePredictionSettings _sspSettings { get; set; }
        protected FeatureVectorCalculatorSettings _fvcSettings { get; set; }


        public MachineLearningController(IMongoDatabase database, IBusControl bus, IKeyValueRepository keyValueRepository, IOptions<SingleStructurePredictionSettings> sspSettings, IOptions<FeatureVectorCalculatorSettings> fvcSettings)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _keyValueRepository = keyValueRepository ?? throw new ArgumentNullException(nameof(keyValueRepository));
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _models = _database.GetCollection<BsonDocument>("Models");
            _accessPermissions = _database.GetCollection<BsonDocument>("AccessPermissions");
            _sspSettings = sspSettings?.Value ?? throw new ArgumentNullException(nameof(sspSettings));
            _fvcSettings = fvcSettings?.Value ?? throw new ArgumentNullException(nameof(fvcSettings));
        }

        [HttpPost("models")]
        [Authorize]
        public async Task<IActionResult> CreateModel([FromBody]CreateMachineLearningModel request)
        {
            Guid trainingId = Guid.NewGuid();
            Guid correlationId = Guid.NewGuid();

            await _bus.Publish(new StartTraining(
                id: request.TargetFolderId,
                parentId: request.TargetFolderId,
                sourceBlobId: request.SourceBlobId,
                scaler: request.Scaler ?? "",
                sourceBucket: request.SourceBucket,
                correlationId: correlationId,
                userId: request.UserId,
                sourceFileName: request.SourceFileName,
                methods: request.Methods,
                className: request.TrainingParameter,
                subSampleSize: request.SubSampleSize,
                testDataSize: request.TestDataSize,
                kFold: request.KFold,
                fingerprints: request.Fingerprints == null ? new List<Dictionary<string, object>>() : request.Fingerprints.Select(f => new Dictionary<string, object>()
                {
                    {"Type", f.Type},
                    {"Size",  f.Size},
                    {"Radius",  f.Radius}
                }),
                optimize: request.Optimize,
                hyperParameters: request.HyperParameters,
                dnnLayers: request.DnnLayers,
                dnnNeurons: request.DnnNeurons

            ));

            return Accepted(new { modelFolderId = request.TargetFolderId, correlationId = correlationId });
        }

        [HttpPost("predictions")]
        [HttpPost("predictions/dataset")]
        [Authorize]
        public async Task<IActionResult> CreatePrediction([FromBody]RunDatasetPrediction request)
        {
            Guid predictionFolderId = request.TargetFolderId;
            Guid correlationId = Guid.NewGuid();

            await _bus.Publish(new CreatePrediction(
                id: predictionFolderId,
                correlationId: correlationId,
                folderId: predictionFolderId,
                datasetBlobId: request.DatasetBlobId,
                datasetBucket: request.DatasetBucket,
                modelBlobId: request.ModelBlobId,
                modelBucket: request.ModelBucket,
                userId: request.UserId
            ));

            return Accepted(new { modelFolderId = predictionFolderId, correlationId = correlationId });
        }

        [HttpPost("predictions/structure")]
        public async Task<IActionResult> CreateSingleStructurePrediction([FromBody]RunSingleStructurePrediction request)
        {
            Guid predictionId = NewId.NextGuid();
            Guid correlationId = NewId.NextGuid();

            if (!(new string[] { "MOL", "SMILES" }.Contains(request.Format)))
            {
                return BadRequest("Provided structure representation type not supported.");
            }

            var document = new Dictionary<string, object>();
            document.Add("predictionId", predictionId);
            document.Add("status", "CALCULATING");
            document.Add("request", new Dictionary<string, object>()
            {
                {"receivedTime", DateTimeOffset.UtcNow },
                {"query", request.Structure },
                {"type", request.Format },
                {"propertyName", request.PropertyName },
                {"models", request.ModelIds }
            });

            var models = new List<Dictionary<string, object>>();

            foreach (var modelId in request.ModelIds)
            {
                BsonDocument entityFilter = new BsonDocument("IsDeleted", new BsonDocument("$ne", true))
                    .Add("_id", modelId);
                var modelViewResult = await _models.Aggregate().Match(entityFilter)
                    .Project<BsonDocument>((new List<string> { "Blob", "Property" }).ToMongoDBProjection())
                    .FirstOrDefaultAsync();

                BsonDocument permissionFilter = new BsonDocument("IsPublic", new BsonDocument("$eq", true))
                    .Add("_id", modelId);
                var modelPermissionResult = await _accessPermissions.Aggregate().Match(permissionFilter).FirstOrDefaultAsync();

                if (modelViewResult != null && modelPermissionResult != null)
                {
                    if (modelViewResult.GetValue("Property").ToBsonDocument().GetValue("Name").ToString() == request.PropertyName)
                    {
                        var blobId = (Guid)modelViewResult.GetValue("Blob").ToBsonDocument().GetValue("_id");
                        var bucket = modelViewResult.GetValue("Blob").ToBsonDocument().GetValue("Bucket").ToString();
                        models.Add(new Dictionary<string, object>()
                            {
                                {"Id", modelId},
                                {"Blob", new Blob { Id =  blobId, Bucket = bucket }}
                            }
                        );
                    }
                    else
                    {
                        return BadRequest($"Prediction can not be created for model with id {modelId} using parameter '{request.PropertyName}'");
                    }
                }
                else
                {
                    return BadRequest($"Model with id {modelId} does not exist or permission denied.");
                }
            }

            _keyValueRepository.SaveObject(predictionId, document);
            _keyValueRepository.SetExpiration(predictionId, TimeSpan.Parse(_sspSettings.RedisExpirationTime));

            await _bus.Publish(new PredictStructure(predictionId, correlationId, request.Structure, request.Format, request.PropertyName, models));

            return Ok(new { predictionId });
            //AcceptedAtRoute("GetPrediction", new { id = predictionId });
        }

        /// <summary>
        /// Get prediction`s status
        /// api/machinelearning/predictions/3df6e978-b22b-4e98-a34a-332a89e06fcd/status
        /// </summary>
        /// <param name="id">entity Id (guid)</param>
        /// <returns></returns>
        [HttpGet("predictions/{id}/status", Name = "GetPredictionStatus")]
        public IActionResult GetPredictionStatus(Guid id)
        {
            var prediction = _keyValueRepository.LoadObject<dynamic>(id);
            if (prediction == null)
            {
                return NotFound();
            }

            return Ok(new { Id = id, Status = prediction["status"] });
        }

        /// <summary>
        /// Get prediction`s report
        /// api/machinelearning/predictions/3df6e978-b22b-4e98-a34a-332a89e06fcd/report
        /// </summary>
        /// <param name="id">entity Id (guid)</param>
        /// <returns></returns>
        [HttpGet("predictions/{id}/report", Name = "GetReport")]
        public IActionResult GetReport(Guid id)
        {
            var report = "Report is not implemented yet.";

            return Ok(report);
        }

        /// <summary>
        /// Get prediction as json string
        /// api/machinelearning/predictions/3df6e978-b22b-4e98-a34a-332a89e06fcd
        /// </summary>
        /// <param name="id">entity Id (guid)</param>
        /// <returns></returns>
        [HttpGet("predictions/{id:guid}", Name = "GetPrediction")]
        public IActionResult GetSingleStructurePrediction(Guid id)
        {
            var prediction = _keyValueRepository.LoadObject<dynamic>(id);
            if (prediction == null)
            {
                return NotFound();
            }

            return Ok(prediction);
        }

        /// <summary>
        /// Update targets for model
        /// </summary>
        /// <param name="id">Model`s identifier</param>
        /// <param name="request">Request with json patch object</param>
        /// <returns></returns>
        /// <response code="202">model`s targets updating started</response>
        [ProducesResponseType(202)]
        [HttpPatch("models/{id}")]
        public async Task<IActionResult> PatchModel(Guid id, [FromBody]JsonPatchDocument<UpdatedModelTargets> request)
        {
            var requestData = new UpdatedModelTargets() { Id = id };

            requestData.UserId = Guid.Empty;
            if (request.Operations.Any(o => o.path.Contains("/UserId")))
            {
                var userId = request.Operations.Where(o => o.path.Contains("/UserId")).Select(result => result.value.ToString()).SingleOrDefault();
                if (userId != null)
                {
                    requestData.UserId = new Guid(userId);
                }
            }

            if (request.Operations.Any(o => o.path.Contains("/Targets")))
            {
                Log.Information($"Changing targets to model {id}");
                var targets = request.Operations.Where(o => o.path.Contains("/Targets")).Select(result => result.value.ToString()).SingleOrDefault();
                if (targets != null)
                {
                    requestData.Targets = JsonConvert.DeserializeObject<List<string>>(targets);

                    await _bus.Publish<SetTargets>(new
                    {
                        Id = id,
                        requestData.UserId,
                        requestData.Targets
                    });
                }
            }

            if (request.Operations.Any(o => o.path.Contains("/ConsesusWeight")))
            {
                Log.Information($"Changing ConsensusWeight field for model {id}");
                var consensusWeight = request.Operations.Where(o => o.path.Contains("/ConsesusWeight")).Select(result => result.value.ToString()).SingleOrDefault();
                if (consensusWeight != null)
                {
                    requestData.ConsensusWeight = Convert.ToDouble(consensusWeight);

                    await _bus.Publish<SetConsensusWeight>(new
                    {
                        Id = id,
                        requestData.UserId,
                        requestData.ConsensusWeight
                    });
                }
            }

            return Ok();
        }

        [HttpPost("features")]
        public async Task<IActionResult> CalculateFeatureVector()
        {
            if (!IsMultipartContentType(Request.ContentType))
                return BadRequest();

            var correlationId = NewId.NextGuid();

            Log.Information("Saving file for feature vector calculating...");

            var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(Request.ContentType).Boundary);
            var reader = new MultipartReader(boundary.Value, Request.Body);

            MultipartSection section;

            IDictionary<string, object> metadata = new Dictionary<string, object>();
            IEnumerable<Fingerprint> fingerprints = null;
            bool isFileLoaded = false;
            string fileExtension = "";
            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var contentDisposition = section.GetContentDispositionHeader();

                if (contentDisposition.IsFormDisposition())
                {
                    var formDataSection = section.AsFormDataSection();

                    if (formDataSection.Name.ToLower() == "fingerprints")
                    {
                        string fpString = await formDataSection.GetValueAsync();
                        fingerprints = JsonConvert.DeserializeObject<IEnumerable<Fingerprint>>(fpString);
                    }
                }

                if (contentDisposition.IsFileDisposition())
                {
                    var fileSection = section.AsFileSection();
                    fileExtension = Path.GetExtension(fileSection.FileName).ToLower();
                    if (!_fvcSettings.SupportedFormats.Contains(fileExtension))
                    {
                        return BadRequest($"File {fileSection.FileName} is not supported for Feature Vector Calculation");
                    }

                    Log.Information($"Saving file {fileSection.FileName}");
                    byte[] source;
                    byte[] buffer = new byte[16 * 1024];
                    using (MemoryStream ms = new MemoryStream())
                    {
                        int read;
                        while ((read = fileSection.FileStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, read);
                        }
                        source = ms.ToArray();
                    }

                    var fileName = $"{correlationId}-file";

                    _keyValueRepository.SaveData(fileName, source);

                    TimeSpan expiry = TimeSpan.Parse(_sspSettings.RedisExpirationTime);

                    _keyValueRepository.SetExpiration(fileName, expiry);

                    isFileLoaded = true;
                }
            }

            if (fingerprints != null && isFileLoaded)
            {
                if (fileExtension == ".cif" && (fingerprints.Count() > 3 || fingerprints.Count() == 0))
                {
                    return BadRequest("Invalid number of fingerprints for Feature Vector Calculation. It should bee between 1 and 3.");
                }

                await _bus.Publish<CalculateFeatureVectors>(new { CorrelationId = correlationId, Fingerprints = fingerprints, FileType = fileExtension.Replace(".", "") });

                return StatusCode((int)HttpStatusCode.Created, new { Id = correlationId });
            }
            else
                return BadRequest();
        }

        private static bool IsMultipartContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// api/machinelearning/features/3df6e978-b22b-4e98-a34a-332a89e06fcd/status
        /// </summary>
        /// <param name="id">entity Id (guid)</param>
        /// <returns></returns>
        [HttpGet("features/{id}/status", Name = "GetFeaturesStatus")]
        public IActionResult GetFeatureStatus(Guid id)
        {
            var src = _keyValueRepository.LoadData($"{id}-file");
            if (src != null)
            {
                var csv = _keyValueRepository.LoadData($"{id}-csv");
                if (csv != null)
                {
                    var result = _keyValueRepository.LoadData($"{id}-result");
                    if(result!=null)
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        using (MemoryStream ms = new MemoryStream(result))
                        {
                            object responce = bf.Deserialize(ms);
                            return Ok(responce);
                        }
                    }
                    else
                    {
                        return Accepted();
                    }
                }
                else
                {
                    var error = _keyValueRepository.LoadData($"{id}-error");
                    if (error != null)
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        using (MemoryStream ms = new MemoryStream(error))
                        {
                            object responce = bf.Deserialize(ms);
                            return BadRequest(responce);
                        }
                    }
                    else
                    {
                        return Accepted();
                    }
                }
            }
            else
                return NotFound();
        }

        /// <summary>
        /// api/machinelearning/features/3df6e978-b22b-4e98-a34a-332a89e06fcd/download
        /// </summary>
        /// <param name="id">entity Id (guid)</param>
        /// <returns></returns>
        [HttpGet("features/{id:guid}/download", Name = "GetFeatureResult")]
        public IActionResult GetFeatureResult(Guid id)
        {
            var src = _keyValueRepository.LoadData($"{id}-file");
            if (src != null)
            {
                var result = _keyValueRepository.LoadData($"{id}-csv");
                if (result != null)
                {
                    Response.Headers.Add("Content-Disposition", $"attachment;filename={id}-result.csv");

                    return File(result, "application/csv");
                }
                else
                {
                    var error = _keyValueRepository.LoadData($"{id}-error");
                    if (error != null)
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        using (MemoryStream ms = new MemoryStream(error))
                        {
                            object responce = bf.Deserialize(ms);
                            return BadRequest(responce);
                        }
                    }
                    else
                    {
                        return Accepted();
                    }
                }
            }
            else
                return NotFound();
        }

        /// <summary>
        /// api/machinelearning/features/3df6e978-b22b-4e98-a34a-332a89e06fcd/preview
        /// </summary>
        /// <param name="id">entity Id (guid)</param>
        /// <returns></returns>
        [HttpGet("features/{id}/preview", Name = "GetFeaturePreview")]
        public IActionResult GetFeature(Guid id, [FromQuery]CsvPreviewRequest request)
        {
            var src = _keyValueRepository.LoadData($"{id}-file");
            if (src != null)
            {
                var resultBytes = _keyValueRepository.LoadData($"{id}-csv");
                if (resultBytes != null)
                {
                    var lines = Regex.Split(Encoding.UTF8.GetString(resultBytes), "\r\n|\r|\n").GetEnumerator();
                    var currentPosition = -1;
                    lines.MoveNext();
                    var result = string.Join(',', lines.Current.ToString().Split(',').Take(request.Columns)) + '\n';
                    while (currentPosition != request.Start)
                    {
                        currentPosition++;
                    }
                    var numOfLines = 0;

                    while (lines.MoveNext() && numOfLines < request.Count)
                    {
                        var columnItems = lines.Current.ToString().Split(',');

                        if (request.Columns > 0)
                        {
                            columnItems = columnItems.Take(request.Columns).ToArray();
                        }

                        result += string.Join(',', columnItems) + '\n';
                        numOfLines++;
                    }

                    return Ok(result);
                }
                else
                {
                    var error = _keyValueRepository.LoadData($"{id}-error");
                    if (error != null)
                    {
                        return BadRequest(Encoding.UTF8.GetString(error));
                    }
                    else
                    {
                        return Accepted();
                    }
                }
            }
            else
                return NotFound();
        }
    }
}