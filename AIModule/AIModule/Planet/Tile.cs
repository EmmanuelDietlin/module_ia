using AIModule.Bases;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Planet
{
    [BsonIgnoreExtraElements]
    public class Tile
    {
        private string itemId;

        [BsonElement("base")]
        public BaseRef Base { get;set; }
	    public bool buildable { get; set; }

        public Tile(bool buildable) { this.buildable = buildable; }

        public Tile() { }
    }
}
