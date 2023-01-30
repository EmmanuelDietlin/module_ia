using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Planet
{
    [BsonIgnoreExtraElements]
    public class Planet : PlanetRef
    {

        public Dictionary<string, Tile> tiles { get; set; } = new Dictionary<string, Tile>();

        public Dictionary<string, int> globalModifier { get; set; } = new Dictionary<string, int>();

        public long version;

        String modifiersId;

        public Planet() { } 
    }
}
