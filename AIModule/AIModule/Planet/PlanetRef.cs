using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Planet
{
    [BsonIgnoreExtraElements]
    public class PlanetRef
    {
        public PlanetRef(string id, string name, string description, int size, bool visible)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.size = size;
            this.visible = visible;
        }

        public PlanetRef() { }  

        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; } 

        
        public string name { get; set; }

        
        public string description { get; set; }

        
        public int size { get; set; }

        
        public bool visible { get; set; }

        public PlanetRef buildRef()
        {
            return new PlanetRef(id, name, description, size, visible);
        }
    }
}
