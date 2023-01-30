using AIModule.Common.constant;
using AIModule.Common.constant.Base;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Events
{
    internal class BuildEvent : Event
    {
        [BsonElement("base")]
        public string baseId { get; set; }               //Base où à lieu la construction

        [BsonElement("building")]
        public string buildingId { get; set; }          //Batiment a construire

        public int lvl { get; set; }

        public int position { get; set; }               //Emplacement de la construction dans la base

        public bool isRepair { get; set; }           //Permet d'indiquer si il s'agit d'une réparation

        public BuildEvent(string baseId, string buildingId, int lvl, int position, DateTime resolution, GlobalPosition location, string description) :
            base()
        {
            this.baseId = baseId;
            this.buildingId = buildingId;
            this.lvl = lvl;
            this.position = position;
            this.type = EventTypeConstant.BUILD;
            this.creation = DateTime.Now;
            this.resolution = resolution;
            this.location = location;
            this.description = description;
            this.isRepair = false;
        }

        public BuildEvent(string baseId, string buildingId,
                          int lvl, int position,
                          DateTime resolution,
                          GlobalPosition location,
                          string description,
                          bool isRepair) : base()
        {
            this.baseId = baseId;
            this.buildingId = buildingId;
            this.lvl = lvl;
            this.position = position;
            this.type = EventTypeConstant.BUILD;
            this.creation = DateTime.Now;
            this.resolution = resolution;
            this.location = location;
            this.description = description;
            this.isRepair = isRepair;
        }
    }
}
