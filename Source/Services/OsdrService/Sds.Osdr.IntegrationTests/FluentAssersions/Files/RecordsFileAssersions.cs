using System;
using FluentAssertions;
using FluentAssertions.Collections;
using Sds.Osdr.Generic.Domain;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Xunit;
using Sds.Osdr.Generic.Extensions;

namespace Sds.Osdr.IntegrationTests.FluentAssersions
{
    public static class RecordsFileAssersionsExtensions
    {
        public static void EntityShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, RecordsFile.Domain.RecordsFile file)
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
                { "SubType", FileType.Records.ToString() },
                { "OwnedBy", file.OwnedBy },
                { "CreatedBy", file.CreatedBy },
                { "CreatedDateTime", file.CreatedDateTime.UtcDateTime },
                { "UpdatedBy", file.UpdatedBy },
                { "UpdatedDateTime", file.UpdatedDateTime.UtcDateTime },
                { "Name", file.FileName },
                { "Status",  file.Status.ToString()},
                { "ParentId", file.ParentId },
                { "Version", file.Version }
            };

            if (assertions.Subject.ContainsKey("TotalRecords"))
            {
                expected.Add("TotalRecords", file.TotalRecords);
            }

            if(file.Status != FileStatus.Failed)
            {
                expected.Add("Properties", new Dictionary<string, object>());

                if(file.Properties.ContainsKey("ChemicalProperties"))
                {
                    ((IDictionary<string, object>)expected["Properties"]).Add("ChemicalProperties", file.Properties["ChemicalProperties"]);
                }
                if (file.Fields != null && file.Fields.Any())
                {
                    ((IDictionary<string, object>)expected["Properties"]).Add("Fields", file.Fields);
                }
            }

            if (file.Images.Any())
            {
                expected.Add("Images", file.Images.Select(i => new Dictionary<string, object>
                {
                    { "_id", i.Id },
                    { "Bucket", file.Bucket },
                    { "Height", i.Height },
                    { "Width", i.Height },
                    { "MimeType", i.MimeType },
                    { "Scale", i.GetScale() }
                }));
            }
			
            assertions.Subject.ShouldAllBeEquivalentTo(expected);
        }

        public static void NodeShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, RecordsFile.Domain.RecordsFile file)
        {
            var expected = new Dictionary<string, object>()
            {
                { "_id", file.Id},
                { "Type", "File" },
                { "SubType", FileType.Records.ToString() },
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
                { "Version", file.Version }
            };

            //if (file.Status != FileStatus.Failed)
            if (assertions.Subject.ContainsKey("TotalRecords"))
            {
                expected.Add("TotalRecords", file.TotalRecords);
            }

            if (file.Images.Any())
            {
                expected.Add("Images", file.Images.Select(i => new Dictionary<string, object>
                {
                    { "_id", i.Id },
                    { "Bucket", file.Bucket },
                    { "Height", i.Height },
                    { "Width", i.Height },
                    { "MimeType", i.MimeType },
                    { "Scale", i.GetScale() }
                }));
            }

            assertions.Subject.ShouldAllBeEquivalentTo(expected);
        }
        public static void NodeShouldBeEquivalentToJson(this GenericCollectionAssertions<Newtonsoft.Json.Linq.JToken> assertions, Newtonsoft.Json.Linq.JObject record)
        {
            var convertToDictionary = new System.Func<Newtonsoft.Json.Linq.JToken, Dictionary<string, object>>(file =>
            {
                var expected = new Dictionary<string, object>()
                {
                    { "id", file["id"]},
                    { "type", file["type"] },
                    { "subType", file["subType"] },
                    { "name", file["name"] },
                    { "blob", new Dictionary<string, object>() {
                        { "id", file["blob"]["id"] },
                        { "bucket", file["blob"]["bucket"] }
                    } },
                    { "ownedBy", file["ownedBy"] },
                    { "createdBy", file["createdBy"] },
                    { "createdDateTime", System.DateTimeOffset.Parse(file["createdDateTime"].ToString()).UtcDateTime },
                    { "updatedBy", file["updatedBy"] },
                    { "updatedDateTime", System.DateTimeOffset.Parse(file["updatedDateTime"].ToString()).UtcDateTime },
                    { "parentId", file["parentId"] },
                    { "version", file["version"] },
                    { "status", file["status"]}
                };

                if (file["images"].Any())
                {
                    expected.Add("images", file["images"].Select(i => new Dictionary<string, object>
                    {
                        { "id", i["id"] },
                        { "width", i["width"] },
                        { "height", i["height"] },
                        { "mimeType", i["mimeType"] },
                        { "scale", i["scale"] }
                    }));
                }
                return expected;
            });

            var dAssertion = convertToDictionary((Newtonsoft.Json.Linq.JToken)assertions.Subject);
            var dRecord = convertToDictionary(record);

            dAssertion.ShouldAllBeEquivalentTo(dRecord);
        }
        public static void ContainsJson(this GenericCollectionAssertions<JToken> asserions, Dictionary<string, object> file)
        {
            var assertionsFile = (JObject)asserions.Subject;

            foreach (var dictionaryFile in file)
            {
                var key = dictionaryFile.Key;
                var value = dictionaryFile.Value;
                var assertValue = assertionsFile[key];

                var valueType = value.GetType();

                if (valueType.Equals(typeof(DateTimeOffset)))
                    value = DateTime.Parse(value.ToString());

                if (!valueType.Equals(typeof(Dictionary<string, object>)) &&
                    !assertValue.Equals(typeof(Dictionary<string, object>)))
                {
                    Assert.Equal(assertValue, value);
                }
                else
                {
                    var assertionDictionaryValue = JsonConvert.DeserializeObject<Dictionary<string, object>>(assertValue.ToString());
                    var fileDictionartValue = (Dictionary<string, object>)value;

                    foreach (var fileFieldDictionary in fileDictionartValue)
                    {
                        var fileFieldKey = fileFieldDictionary.Key;
                        var fileFieldValue = fileFieldDictionary.Value.ToString();
                        var asserionFieldValue = assertionDictionaryValue[fileFieldKey].ToString();

                        Assert.Equal(fileFieldValue, asserionFieldValue);
                    }
                }
            }
        }
        public static void ContainsJson(this GenericCollectionAssertions<JToken> asserions, JObject file, List<string> ignoreFields = null)
        {
            var assertionsFile = (JObject)asserions.Subject;

            foreach (var dictionaryFile in file)
            {
                var key = dictionaryFile.Key;
                var value = dictionaryFile.Value;
                var assertValue = assertionsFile[key];
                var valueType = value.GetType();

                if (ignoreFields != null)
                {
                    if (!ignoreFields.Contains(key))
                        CheckJsonFile(key, value, assertValue, valueType);
                }
                else CheckJsonFile(key, value, assertValue, valueType);
            }
        }
        public static void ContainsJson(this GenericCollectionAssertions<JToken> asserions, string file, List<string> ignoreFields = null)
        {
            file = file.Replace("*EXIST*", "'*no_valid_field*'");
            ContainsJson(asserions, JObject.Parse(file), ignoreFields);
        }
        public static void ContainsJson(this GenericDictionaryAssertions<string, JToken> assertions, string file, List<string> ignoreFields = null)
        {
            file = file.Replace("*EXIST*", "'*no_valid_field*'");
            var json = assertions.Subject.ToString();
            var token = JToken.Parse(json);

            var assertionsCollection = new GenericCollectionAssertions<JToken>(token);
            ContainsJson(assertionsCollection, file, ignoreFields);
        }
        private static void CheckJsonFile(string key, JToken value, JToken assertValue, Type valueType)
        {
            if (valueType.Equals(typeof(JValue)))
            {
                if (!value.ToString().Equals("*no_valid_field*"))
                    if (!CheckDateTime(key, value, assertValue))
                        if (assertValue == null)
                            Assert.False(true, $"Field[{key}] - not found field");
                        else Assert.True(assertValue.ToString().Equals(value.ToString()), $"The field is not equivalent\nField [{key}]: {assertValue} is not equal to {value}");
            }
            else if (valueType.Equals(typeof(JObject)))
            {
                if (assertValue == null)
                {
                    Assert.False(true, $"Field[{key}] - not found field");
                }

                Assert.False(assertValue.GetType().Equals(typeof(JValue)), $"Object [{key}] not found value");
                var assertionDictionaryValue = JsonConvert.DeserializeObject<Dictionary<string, object>>(assertValue.ToString());
                var fileDictionartValue = JsonConvert.DeserializeObject<Dictionary<string, object>>(value.ToString());

                foreach (var fileFieldDictionary in fileDictionartValue)
                {
                    var fileFieldKey = fileFieldDictionary.Key;
                    var fileFieldValue = fileFieldDictionary.Value.ToString();
					
                    Assert.True(assertionDictionaryValue.ContainsKey(fileFieldKey), $"Is not found property [{fileFieldKey}] in object [{key}]");

                    var asserionFieldValue = assertionDictionaryValue[fileFieldKey].ToString();
                    var isArray = assertionDictionaryValue[fileFieldKey].GetType() == typeof(JArray);

                    if (isArray)
                    {
                        if (!fileFieldValue.ToString().Equals("*no_valid_field*"))
                            CheckJsonFile(fileFieldKey, JToken.Parse(fileFieldValue), JToken.Parse(asserionFieldValue), typeof(JArray));
                    } else Assert.True(fileFieldValue.Equals(asserionFieldValue), $"The object {fileFieldValue} is not equivalent\nField {fileFieldKey} [{key}]: {fileFieldValue} is not equal to {asserionFieldValue}");
                }
            }
            else if (valueType.Equals(typeof(JArray)))
            {
                JArray arrayFileProperty = (JArray)value;
                JArray arrayAssertProperty = (JArray)assertValue;

                for (int i = 0; i < arrayAssertProperty.Count; i++)
                {
                    var fileProperty = arrayFileProperty[i];
                    object assertProperty = null;

                    for (var j = 0; j < arrayAssertProperty.Count; j++)
                        if (fileProperty.ToString().Equals(arrayAssertProperty[j].ToString()))
                            assertProperty = arrayAssertProperty[j];

                    Assert.True(assertProperty != null, $"Field [{key}] is not found property [{fileProperty}]");
					
                    if (arrayAssertProperty[i].GetType() == typeof(JValue))
                    {
                        var jTokenAssertProperty = assertProperty as JToken;
                        var assertGenericCollection = new GenericCollectionAssertions<JToken>(jTokenAssertProperty);

                        CheckJsonFile(key, fileProperty, jTokenAssertProperty, fileProperty.GetType());
                    }
                    else if (arrayAssertProperty[i].GetType() == typeof(JObject))
                    {
                        var jObjectAssertProperty = assertProperty as JObject;
                        var assertGenericCollection = new GenericCollectionAssertions<JToken>(jObjectAssertProperty);

                        ContainsJson(assertGenericCollection, fileProperty as JObject);
                    }
                    else
                    {
                        var jArrayAssertProperty = assertProperty as JArray;
                        var assertGenericCollection = new GenericCollectionAssertions<JToken>(jArrayAssertProperty);

                        ContainsJson(assertGenericCollection, fileProperty.ToString());
                    }
                }
            }
        }
        private static bool CheckDateTime(string key, JToken value, JToken assertValue)
        {
            var dateValue = new DateTimeOffset();
            bool isDateTime = DateTimeOffset.TryParse(value.ToString(), out dateValue);

            if (!isDateTime)
                return false;

            var dateAssertion = DateTimeOffset.Parse(assertValue.ToString());
            var percision = dateValue.AddSeconds(10);

            string errorMessage = $"The field is not equivalent\nField [{key}]: {assertValue} is not equal to {value}";

            if (dateAssertion <= dateValue && dateAssertion <= percision)
            {
            } else Assert.True(false, errorMessage);

            return isDateTime;
        }
    }
}