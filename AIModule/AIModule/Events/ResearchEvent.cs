using AIModule.Common.constant.Base;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Events
{
    internal class ResearchEvent : Event
    {
        [BsonElement("base")]
        public string baseId { get; set; }               //Base où à lieu la recherche
        
        [BsonElement("research")]
        public string researchId { get; set; }          //Recherche en cours

        [BsonElement("lvl")]
        public int lvl { get; set; }

        public ResearchEvent(string baseId, string researchId, int lvl, DateTime resolution, GlobalPosition location, string description) :
            base()
        {
            this.baseId = baseId;
            this.researchId = researchId;
            this.lvl = lvl;
            this.creation = DateTime.Now;
            this.resolution = resolution;
            this.location = location;
            this.description = description;
            this.userIds = new HashSet<string>();
        }
    }
}
