using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization.Options;

namespace AIModule.Bases
{
    [BsonIgnoreExtraElements]
    public class BaseSlot
    {
        
        public string type { get; set; }

        public Built built { get; set; }

        //indique si le batiment est en cours de construction
        //[BsonDateTimeOptions(Representation = BsonType.String)]
        public DateTime? construction { get; set; }    //null si pas de construction

        public BaseSlot(string type)
        {
            this.type = type;
            this.built = null;
            this.construction = null;
        }

        public BaseSlot() { }

        public BaseSlot(string type, Built built, DateTime construction) : this(type)
        {
            this.built = built;
            this.construction = construction;
        }
    }
}
