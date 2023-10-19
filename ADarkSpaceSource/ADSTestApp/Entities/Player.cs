using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Numerics;

namespace ADSTestApp.Entities
{
    public class Player
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        [BsonElement("Name")]
        public string Name { get; set; } = "";
        
        public Vector3 Position { get; set; } = Vector3.Zero;

        [BsonElement("PositionX")]
        public float PositionX { get => Position.X; set => Position = new Vector3(value, Position.Y, Position.Z); }
        [BsonElement("PositionY")]
        public float PositionY { get => Position.Y; set => Position = new Vector3(Position.X, value, Position.Z); }
        [BsonElement("PositionZ")]
        public float PositionZ { get => Position.Z; set => Position = new Vector3(Position.X, Position.Y, value); }
    }
}
