using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Numerics;

namespace ADSCommon.Entities
{
    public class Player
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        [BsonElement("Name")]
        public string Name { get; set; } = "";

        [BsonElement("ShipId")]
        public string ShipId { get; set; } = "";

        [BsonElement("Score")]
        public int Score { get; set; } = 0;
        [BsonElement("HiScore")]
        public int HiScore { get; set; } = 0;
        [BsonElement("Kills")]
        public int Kills { get; set; } = 0;
        [BsonElement("Deaths")]
        public int Deaths { get; set; } = 0;
        [BsonElement("Shots")]
        public int Shots { get; set; } = 0;
        [BsonElement("Hits")]
        public int Hits { get; set; } = 0;
        [BsonElement("Misses")]
        public int Misses { get; set; } = 0;
        [BsonElement("Dodges")]
        public int Dodges { get; set; } = 0;

    }
}
