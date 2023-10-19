using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Numerics;

namespace ADSTestApp.Entities
{
    public class Ship
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        [BsonElement("Name")]
        public string Name { get; set; } = "";

        public static List<string> ShipTypes { get; } = new List<string> 
        { 
            "X-Wing", "Y-Wing", "B-Wing", "A-Wing", 
            "TIE Fighter", "TIE Interceptor", "TIE Bomber", "TIE Advanced", 
            "TIE Defender", "Shuttle", "Starwing" 
        };

        [BsonElement("ShipType")]
        public string ShipType { get; set; }    = "X-Wing";


        [BsonElement("PositionX")]
        public float PositionX { get; set; }    = 0f;
        [BsonElement("PositionY")]
        public float PositionY { get; set; }    = 0f;

        [BsonElement("Rotation")]
        public float Rotation { get; set; }     = 0f;

        [BsonElement("Speed")]
        public float Speed { get; set; }        = 100f;

        [BsonElement("RotationSpeed")]
        public float RotationSpeed { get; set; } = 0f;

    }
}
