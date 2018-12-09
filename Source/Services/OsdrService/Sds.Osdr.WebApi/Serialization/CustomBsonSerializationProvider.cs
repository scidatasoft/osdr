using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Sds.Osdr.WebApi.Serialization
{
    class CustomBsonSerializationProvider : IBsonSerializationProvider
    {
        public IBsonSerializer GetSerializer(Type type)
        {
            if (type == typeof(BsonDateTime))
            {
                return new CustomBsonDateTimeSerializer();
            }

            return null;
        }
    }
}
