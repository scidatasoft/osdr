using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Sds.Osdr.WebApi.Serialization
{
    class CustomBsonDateTimeSerializer : BsonDateTimeSerializer
    {
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, BsonDateTime value)
        {
            context.Writer.WriteString(value.ToString());
        }
    }
}
