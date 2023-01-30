using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIModule.Common.constant;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AIModule.Events
{
    internal class BuildShipEvent : Event
    {
        [BsonElement("base")]
        public string baseId { get; set; }               //Base où à lieu la construction

        [BsonElement("building")]
        public string shipId { get; set; }          //Batiment à construire

        [BsonElement("amount")]
        public int amount { get; set; }                 //Nombre restant à construire

        [BsonElement("total")]
        public int total { get; set; }                  //Nombre qu'il y avait a construie au départ

        public bool turret { get; set; }

        public BuildShipEvent(bool turret) : base()
        {
            this.type = EventTypeConstant.BUILD_SHIP;
            this.creation = DateTime.Now;
            this.turret = turret;
        }
    }
}
