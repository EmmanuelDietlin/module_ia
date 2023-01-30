using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Bases
{
    [BsonIgnoreExtraElements]
    public class BuildingRef
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }

        /*Building generic statistics*/
        public string name { get; set; }

        public string description { get; set; }

        public string techType { get; set; }

        public bool hasExternalEffect { get; set; }

        public BuildingRef(string id, string name, string description, string techType, bool hasExternalEffect)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.techType = techType;
            this.hasExternalEffect = hasExternalEffect;
        }

        public BuildingRef() { } 

    }
}
