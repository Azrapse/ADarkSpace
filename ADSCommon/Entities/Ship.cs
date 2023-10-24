using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Numerics;

namespace ADSCommon.Entities
{
    public class Ship
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        
                
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("SectorId")]
        public string SectorId { get; set; } = "";

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


        [BsonElement("StartPositionX")]
        public float StartPositionX { get; set; }    = 0f;
        [BsonElement("StartPositionY")]
        public float StartPositionY { get; set; }    = 0f;
        [BsonIgnore]
        public Vector2 StartPosition
        {
            get => new(StartPositionX, StartPositionY);
            set 
            {
                StartPositionX = value.X;
                StartPositionY = value.Y;
            }
        }

        [BsonElement("EndPositionX")]
        public float EndPositionX { get; set; } = 0f;
        [BsonElement("EndPositionY")]
        public float EndPositionY { get; set; } = 0f;
        [BsonIgnore]
        public Vector2 EndPosition
        {
            get => new(EndPositionX, EndPositionY);
            set
            {
                EndPositionX = value.X;
                EndPositionY = value.Y;
            }
        }

        [BsonElement("StartForwardX")]
        public float StartForwardX { get; set; }     = 1f;
        [BsonElement("StartForwardY")]
        public float StartForwardY { get; set; } = 0f;
        [BsonIgnore]
        public Vector2 StartForward {
            get => new(StartForwardX, StartForwardY); 
            set
            {
                StartForwardX = value.X;
                StartForwardY = value.Y;
            }
        }


        [BsonElement("EndForwardX")]
        public float EndForwardX { get; set; } = 1f;
        [BsonElement("EndForwardY")]
        public float EndForwardY { get; set; } = 0f;

        [BsonIgnore]
        public Vector2 EndForward {
            get => new(EndForwardX, EndForwardY);
            set
            {
                EndForwardX = value.X;
                EndForwardY = value.Y;
            }
        }

        [BsonElement("Speed")]
        public float Speed { get; set; }        = 100f;

        [BsonElement("TargetTurn")]
        public float TargetTurn { get; set; } = 0f;
        [BsonElement("MovementStartTime")]
        public long MovementStartTime { get; set; } = -1;
        [BsonElement("MovementEndTime")]
        public long MovementEndTime { get; set; } = -1;
    }
}
