using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AIModule.Owners
{
    [BsonIgnoreExtraElements]
    public class AllianceRef : Owner
    {
        public string leaderId { get; set; }

        public string leaderName { get; set; }

        public string shortName { get; set; }

        public AllianceRef(string id,
                           string name,
                           string description,
                           string leaderId,
                           string leaderName,
                           string shortName,
                           string imageLocation)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.leaderId = leaderId;
            this.leaderName = leaderName;
            this.shortName = shortName;
            this.imageLocation = imageLocation;
        }

        public AllianceRef buildRef() { return new AllianceRef(id, name, description, leaderId, leaderName, shortName, imageLocation); }

        public AllianceRef() { }
    }
}
