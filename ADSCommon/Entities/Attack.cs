using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ADSCommon.Entities
{
    public class Attack
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        
        [BsonRepresentation(BsonType.ObjectId)]
        public string AttackerId { get; set; } = "";

        [BsonRepresentation(BsonType.ObjectId)]
        public string TargetId { get; set; } = "";

        [BsonElement("Amount")]
        public int Amount { get; set; } = 0;

        [BsonElement("Weapon")]
        // Red Laser, Green Laser, Missile, Torpedo
        public string Weapon { get; set; } = "";

        [BsonElement("Damage")]
        public float Damage { get; set; } = 0f;


        
        [BsonElement("Result")]        
        // Hit or Miss
        public string Result { get; set; } = "";
    }
}
