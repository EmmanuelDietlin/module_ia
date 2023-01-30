using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Reputation
{
    [BsonIgnoreExtraElements]
    public class ReputationObject
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }
        public Dictionary<string, float> reputationValues { get; set; }
        public long version { get; set; }

        public ReputationObject() { }
    }
}
