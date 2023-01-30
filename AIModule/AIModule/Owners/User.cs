using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Owners
{
    [BsonIgnoreExtraElements]
    public class User : UserRef
    {
        public string governmentId { get; set; }

        public string allianceId { get; set; }

        public string allianceName { get; set; }

        public bool active { get; set; }

        public DateTime lastConnect { get; set; }

        public string uniqueId { get; set; }

        public Dictionary<string, bool> reports { get; set; }

        public Dictionary<string, string> storedReports { get; set; }

        public int centaurium { get; set; }

        public string confId { get; set; }

        public int aiQuestsQuantity { get; set; }

        public User(string id, string name, string description) : base(id, name, description)
        {
        }

        public User() { }
    }
}
