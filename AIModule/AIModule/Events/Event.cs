using AIModule.Common.constant.Base;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AIModule.Events
{
    [BsonIgnoreExtraElements]
    public class Event
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }

        [BsonElement("type")]
        public string type { get; set; }

        [BsonElement("creation")]
        public DateTime creation { get; set; }

        [BsonElement("resolution")]
        public DateTime resolution { get; set; }

        [BsonElement("userIds")]
        public HashSet<string> userIds { get; set; }     //Utilisateurs concernés par cet l'évènement

        [BsonElement("location")]
        public GlobalPosition location { get; set; } //Emplacement où l'event aura lieu, permet de traiter les évènements d'une location dans l'ordre

        [BsonElement("description")]
        public string description { get; set; }

        public bool visibleByLocation { get; set; } = true;

        [JsonPropertyName("hostile")]
        public bool isHostile { get; set; } = false;

        [JsonIgnore]
        public string _class { get; set; }

        public Event(string id, string type, DateTime creation, DateTime resolution, HashSet<string> userIds, GlobalPosition location, string description)
        {
            this.id = id;
            this.type = type;
            this.creation = DateTime.SpecifyKind(creation, DateTimeKind.Unspecified);
            this.resolution = DateTime.SpecifyKind(resolution, DateTimeKind.Unspecified);
            this.userIds = userIds;
            this.location = location;
            this.description = description;
        }

        public Event()
        {
            
        }
        
        public Event(Event evnt)
        {
            this.id = evnt.id;
            this.type = evnt.type;
            this.creation = evnt.creation;
            this.resolution = evnt.resolution;
            this.userIds = evnt.userIds;
            this.location = evnt.location;
            this.description = evnt.description;
            this.visibleByLocation= evnt.visibleByLocation;
            this.isHostile= evnt.isHostile;
            this._class = evnt._class;
        } 
    }
}
