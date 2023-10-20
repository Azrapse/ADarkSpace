using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Numerics;

namespace ADSTestApp.Entities
{
    public class Sector
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        
        [BsonElement("Name")]
        public string Name { get; set; } = "";
        
        [BsonElement("LastUpdateTime")]
        public long LastUpdateTime { get; set; } = long.MinValue;
        
        [BsonElement("NextUpdateTime")]
        public long NextUpdateTime { get; set; } = long.MinValue;
                
        [BsonElement("UpdatedBy")]
        public string UpdatedBy { get; set; } = "None";

    }
}
