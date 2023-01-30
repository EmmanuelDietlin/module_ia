using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace AIModule.Owners
{
    [BsonIgnoreExtraElements]
    public abstract class Owner
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }
        
        [BsonElement("name")]
        public string name { get; set; }

        [BsonElement("description")]
        public string description { get; set; }

        [BsonElement("mainBase")]
        public string mainBaseId { get; set; }

        [BsonElement("baseList")]
        public Dictionary<string, string> baseListId { get; set; } = new Dictionary<string, string>();

        [BsonElement("fleetListId")]
        public List<string> fleetListId { get; set; } = new List<string>();

        [BsonElement("agentListId")]
        public List<string> agentListId { get; set; } = new List<string>();

        public Dictionary<string, int> agentAmountPerType { get; set; } = new Dictionary<string, int>();

        [BsonElement("money")]
        public int money { get; set; }

        public Dictionary<string, int> virtualResources { get; set; } = new Dictionary<string, int>();       //Les monaies, entre autre

        [BsonElement("effects")]
        public Dictionary<string, int> effects { get; set; } = new Dictionary<string, int>();

        public Dictionary<string, string> effectSources { get; set; } = new Dictionary<string, string>();

        public Dictionary<string, int> researchMap { get; set; } = new Dictionary<string, int>();

        public List<string> enemyIds { get; set; } = new List<string>();

        public List<string> allyIds { get; set; } = new List<string>();

        [BsonElement("type")]
        public string type { get; set; }

        public string diplomacyId { get; set; }

        public Dictionary<string, DateTime> pendingResearch { get; set; } = new Dictionary<string, DateTime>();

        public string imageLocation { get; set; }

        public long version { get; set; }

        public string _class { get; set; }

        protected Owner() { }
        protected Owner(string id, string name, string description, 
            string mainBaseId, Dictionary<string, string> baseListId, 
            List<string> fleetListId, List<string> agentListId, 
            Dictionary<string, int> agentAmountPerType, int money, 
            Dictionary<string, int> virtualResources, Dictionary<string, int> effects, 
            Dictionary<string, string> effectSources, Dictionary<string, int> researchMap, 
            string type, string diplomacyId, Dictionary<string, DateTime> pendingResearch, 
            string imageLocation, long version)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.mainBaseId = mainBaseId;
            this.baseListId = baseListId;
            this.fleetListId = fleetListId;
            this.agentListId = agentListId;
            this.agentAmountPerType = agentAmountPerType;
            this.money = money;
            this.virtualResources = virtualResources;
            this.effects = effects;
            this.effectSources = effectSources;
            this.researchMap = researchMap;
            this.type = type;
            this.diplomacyId = diplomacyId;
            this.pendingResearch = pendingResearch;
            this.imageLocation = imageLocation;
            this.version = version;
        }
    }
}
