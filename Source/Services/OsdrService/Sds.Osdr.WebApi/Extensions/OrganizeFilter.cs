using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sds.Osdr.WebApi.Extensions
{
    public class OrganizeFilter
    {
        BsonDocument _filter = new BsonDocument("IsDeleted", new BsonDocument("$ne", true));

        public OrganizeFilter()
        {
            
        }

        public OrganizeFilter(Guid userId)
        {
            _filter.Add("OwnedBy", userId);
        }

        public OrganizeFilter ById(Guid id)
        {
            _filter.Add("_id", id);

            return this;
        }

        public OrganizeFilter ByParent(Guid? parentId)
        {
            if (parentId.HasValue)
                _filter.Add("ParentId", parentId);
            else
                _filter.Add("ParentId", BsonNull.Value);

            return this;
        }

        public OrganizeFilter ByQueryString(string s) // TODO: Refactoring for user empty
        {
            if (!string.IsNullOrEmpty(s))
            {
                foreach (var condition in s.Trim('[', ']').Split(new[] { "and" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var conditionArray = condition.Trim().Split(new[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);

                    if (!conditionArray[1].StartsWith("$"))
                        conditionArray[1] = $"${conditionArray[1]}";

                    if (bool.TryParse(conditionArray[2], out bool val) && string.Equals(conditionArray[1], "$eq"))
                    {
                        _filter.Add(conditionArray[0].ToPascalCase(), val);
                        continue;
                    }

                    if (string.Equals(conditionArray[1], "$eq"))
                        conditionArray[1] = "$in";

                    if (string.Equals(conditionArray[1], "$in"))
                    {
                        if (string.Equals(conditionArray[0], "blob.id", StringComparison.OrdinalIgnoreCase))
                        {
                            _filter.Add("Blob._id", new BsonDocument(conditionArray[1],
                                new BsonArray(conditionArray[2].Trim('(', ')', ' ').Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(str => Guid.Parse(str.Trim('\''))))));
                        }
                        else
                        {
                            var values = conditionArray[2].Trim('(', ')', ' ').Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            _filter.Add(conditionArray[0].ToPascalCase(), new BsonDocument(conditionArray[1], 
                                new BsonArray(values.Select(str => conditionArray[0].EndsWith("dateTime", StringComparison.OrdinalIgnoreCase) 
                                    ? DateTime.Parse(str)
                                    : (str.Trim().StartsWith("'") 
                                        ? BsonRegularExpression.Create(new Regex($"^{str.Trim('\'', ' ')}$", RegexOptions.IgnoreCase)) 
                                        : (object)long.Parse(str))))));
                        }
                    }
                    else
                    {
                        if (!conditionArray[0].EndsWith("dateTime", StringComparison.OrdinalIgnoreCase))
                        {
                            if (conditionArray[2].StartsWith("'"))
                                _filter.Add(conditionArray[0].ToPascalCase(), new BsonDocument(conditionArray[1], BsonRegularExpression.Create(new Regex($"^{conditionArray[2].Trim('\'')}$", RegexOptions.IgnoreCase))));
                            else
                                _filter.Add(conditionArray[0].ToPascalCase(), new BsonDocument(conditionArray[1], long.Parse(conditionArray[2])));
                        }
                        else
                        {
                            if (!(_filter.Contains(conditionArray[0].ToPascalCase())))
                                _filter.Add(conditionArray[0].ToPascalCase(), new BsonDocument(conditionArray[1], DateTime.Parse(conditionArray[2])));
                            else
                            {
                                _filter[conditionArray[0].ToPascalCase()].AsBsonDocument.Add(conditionArray[1], DateTime.Parse(conditionArray[2]));
                            }
                        }
                    }
                }
            }

            return this;
        }

        public static implicit operator BsonDocument(OrganizeFilter filter)
        {
            return filter._filter;
        }

        public static implicit operator FilterDefinition<BsonDocument>(OrganizeFilter filter)
        {
            return filter._filter;
        }
    }
}
